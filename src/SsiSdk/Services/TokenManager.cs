using System.Globalization;
using System.Text.Json;
using SsiSdk.Internal;
using SsiSdk.Models;
using SsiSdk.Transport;

namespace SsiSdk.Services;

public sealed class TokenManager
{
    private static readonly Logger Log = new("ssi_sdk.auth");

    private readonly RestClient _rest;
    private readonly Config _config;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private Token? _token;

    internal TokenManager(RestClient rest, Config config)
    {
        _rest = rest;
        _config = config;
    }

    /// <summary>Clock in epoch seconds; replaceable in tests.</summary>
    internal Func<double> Now { get; set; } = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

    public Token? Token => _token;
    public string AccessToken => _token?.AccessToken ?? string.Empty;

    /// <summary>No token, unknown expiry, or within 30 seconds of expiry.</summary>
    public bool IsTokenExpired
    {
        get
        {
            var token = _token;
            if (token is null || token.ExpiresAt <= 0) return true;
            return Now() >= token.ExpiresAt - Constants.TokenExpirySkewMs / 1000.0;
        }
    }

    public bool HasRefreshToken => _token is not null && !string.IsNullOrEmpty(_token.RefreshToken);

    public bool CanRefresh => _token is not null && _token.CanRefreshAt(Now());

    public bool IsRefreshTokenExpired => !CanRefresh;

    /// <summary>Single call to the token endpoint. Pass at most one of <paramref name="otp"/> or <paramref name="transactionId"/>.</summary>
    /// <exception cref="SmartOtpPendingException">The Smart OTP push has not been approved yet.</exception>
    public async Task<Token> AuthenticateAsync(string? otp = null, string? transactionId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_config.ApiKey) || string.IsNullOrEmpty(_config.ApiSecret))
            throw new AuthenticationException("api_key and api_secret are required for authentication");
        if (!string.IsNullOrEmpty(otp) && !string.IsNullOrEmpty(transactionId))
            throw new AuthenticationException("Pass only one of otp or transaction_id, not both");

        var request = new TokenRequest
        {
            ApiKey = _config.ApiKey,
            ApiSecret = _config.ApiSecret,
            Otp = string.IsNullOrEmpty(otp) ? null : otp,
            TransactionId = string.IsNullOrEmpty(transactionId) ? null : transactionId,
        };
        var data = await _rest.PostAsync(Constants.EpAccessToken, request.ToDictionary(), ct: ct, retry: false).ConfigureAwait(false);
        _token = ParseToken(data, "authenticating");
        _rest.SetAuthHeader(_token.AccessToken);
        Log.Info("Authentication successful");
        return _token;
    }

    /// <summary>Forces a refresh with the stored refresh token.</summary>
    public Task<Token> RefreshAsync(CancellationToken ct = default) => RefreshCoreAsync(force: true, ct);

    private async Task<Token> RefreshCoreAsync(bool force, CancellationToken ct)
    {
        CheckRefreshable();
        var observed = _token?.AccessToken;

        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Another caller may have refreshed while we waited for the lock.
            if (Coalesced(observed) || (!force && !IsTokenExpired))
                return _token!;
            CheckRefreshable();

            if (string.IsNullOrEmpty(_config.ApiKey) || string.IsNullOrEmpty(_config.ApiSecret))
                throw new AuthenticationException("api_key and api_secret are required for token refresh");
            var body = new RefreshTokenRequest { RefreshToken = _token!.RefreshToken }.ToDictionary();

            JsonElement data;
            try
            {
                data = await _rest.PostAsync(Constants.EpRefreshToken, body, ct: ct, retry: false).ConfigureAwait(false);
            }
            catch (SsiException ex) when (ex is AuthenticationException or ApiException && IsReauthError(ex))
            {
                ClearToken();
                throw new ReauthenticationRequiredException(
                    "Refresh token rejected by the server — authenticate again with a new OTP",
                    ex.Code, ex.StatusCode, ex.ResponseBody, ex.Headers);
            }

            _token = ParseToken(data, "refreshing");
            _rest.SetAuthHeader(_token.AccessToken);
        }
        finally
        {
            _refreshLock.Release();
        }
        Log.Info("Token refreshed successfully");
        return _token;
    }

    public async Task<JsonElement> RequestOtpAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_config.ApiKey) || string.IsNullOrEmpty(_config.ApiSecret))
            throw new AuthenticationException("api_key and api_secret are required for OTP request");

        var body = new OtpRequest { ApiKey = _config.ApiKey, ApiSecret = _config.ApiSecret }.ToDictionary();
        var data = await _rest.PostAsync(Constants.EpRequestOtp, body, ct: ct, retry: false).ConfigureAwait(false);
        if (data.ValueKind != JsonValueKind.Object)
            throw new ApiException("Unexpected response format while requesting OTP");
        return data;
    }

    public async Task<OtpResponse> RequestOtpTypedAsync(CancellationToken ct = default) =>
        OtpResponse.FromJson(await RequestOtpAsync(ct).ConfigureAwait(false));

    public void SetToken(Token token)
    {
        _token = token;
        _rest.SetAuthHeader(token.AccessToken);
        Log.Info("Access token set manually");
    }

    /// <summary>
    /// Returns a valid access token. Order: current token if still valid, then refresh, then OTP,
    /// then Smart OTP polling. A token that cannot be refreshed raises <see cref="ReauthenticationRequiredException"/>.
    /// </summary>
    public async Task<string> EnsureAuthenticatedAsync(
        string? otp = null,
        string? transactionId = null,
        int? pollIntervalMs = null,
        int? pollMaxRetries = null,
        CancellationToken ct = default)
    {
        if (_token is not null && !IsTokenExpired)
            return _token.AccessToken;

        if (_token is not null && CanRefresh)
            await RefreshCoreAsync(force: false, ct).ConfigureAwait(false);
        else if (!string.IsNullOrEmpty(otp))
            await AuthenticateAsync(otp: otp, ct: ct).ConfigureAwait(false);
        else if (!string.IsNullOrEmpty(transactionId))
            await PollSmartOtpAsync(transactionId, pollIntervalMs, pollMaxRetries, ct).ConfigureAwait(false);
        else if (_token is not null)
            throw new ReauthenticationRequiredException("Session expired and cannot be refreshed — authenticate again with a new OTP");
        else
            throw new AuthenticationException("OTP or Smart OTP transaction_id is required to authenticate — no token available");

        return _token!.AccessToken;
    }

    internal static (int WaitMs, int Attempts) PollAttempts(Config config, int? intervalMs, int? maxRetries)
    {
        var wait = intervalMs ?? config.OtpPollIntervalMs;
        if (maxRetries is not null) return (wait, Math.Max(1, maxRetries.Value));
        if (wait <= 0) return (wait, 1);
        return (wait, Math.Max(1, (int)Math.Ceiling((double)config.OtpPollMaxWaitMs / wait)));
    }

    private async Task<Token> PollSmartOtpAsync(string transactionId, int? intervalMs, int? maxRetries, CancellationToken ct)
    {
        var (wait, attempts) = PollAttempts(_config, intervalMs, maxRetries);
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                return await AuthenticateAsync(transactionId: transactionId, ct: ct).ConfigureAwait(false);
            }
            catch (SsiException ex) when (ex is AuthenticationException or ApiException)
            {
                if (!IsSmartOtpPending(ex)) throw;
                if (attempt >= attempts)
                    throw new SmartOtpPendingException(
                        $"Smart OTP approval not confirmed after {attempts} attempts — ask the user to approve the request on their device, then call again with the same transaction_id",
                        ex.Code, ex.StatusCode);
                Log.Info($"Smart OTP approval still pending (attempt {attempt}/{attempts}), retrying in {wait} ms...");
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(wait, 0)), ct).ConfigureAwait(false);
            }
        }
        throw new AuthenticationException("Smart OTP polling failed");
    }

    private void CheckRefreshable()
    {
        if (!HasRefreshToken)
            throw new AuthenticationException("No refresh token available — authenticate first");
        if (!CanRefresh)
            throw new ReauthenticationRequiredException("Refresh token expired — authenticate again with a new OTP");
    }

    private void ClearToken()
    {
        _token = null;
        _rest.ClearAuthHeader();
    }

    private bool Coalesced(string? observedAccessToken) =>
        _token is not null && _token.AccessToken != observedAccessToken && !IsTokenExpired;

    internal static bool IsSmartOtpPending(SsiException ex)
    {
        if (ex is SmartOtpPendingException) return true;
        return ex.ResponseBody is not null
            && ex.ResponseBody.TryGetValue("code", out var code)
            && Converter.ToStr(code) == Constants.SmartOtpPendingCode.ToString(CultureInfo.InvariantCulture);
    }

    internal static bool IsReauthError(SsiException ex) =>
        int.TryParse(ex.Code, NumberStyles.Integer, CultureInfo.InvariantCulture, out var c)
        && Constants.RefreshReauthCodes.Contains(c);

    internal static Token ParseToken(JsonElement data, string context)
    {
        var body = ToBody(data);
        if (data.ValueKind != JsonValueKind.Object)
            throw new ApiException($"Unexpected response format while {context}", responseBody: body);
        var payload = data.TryGetProperty("data", out var d) ? d : data;
        if (payload.ValueKind != JsonValueKind.Object)
            throw new ApiException($"Unexpected token payload format while {context}", responseBody: body);
        var token = Token.FromJson(payload);
        if (string.IsNullOrEmpty(token.AccessToken))
            throw new ApiException($"{char.ToUpperInvariant(context[0])}{context[1..]} payload is missing access token", responseBody: body);
        return token;
    }

    private static Dictionary<string, JsonElement>? ToBody(JsonElement data) =>
        data.ValueKind == JsonValueKind.Object
            ? data.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone())
            : null;
}

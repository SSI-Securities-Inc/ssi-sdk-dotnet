using System.Text.Json;
using SsiSdk.Internal;
using SsiSdk.Models;
using SsiSdk.Transport;

namespace SsiSdk.Services;

public sealed class TokenManager
{
    private static readonly Logger Log = new("ssi_sdk.auth");

    private readonly RestClient _rest;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private Token? _token;

    internal TokenManager(RestClient rest, string apiKey, string apiSecret)
    {
        _rest = rest;
        _apiKey = apiKey;
        _apiSecret = apiSecret;
    }

    public Token? Token => _token;
    public string AccessToken => _token?.AccessToken ?? string.Empty;

    public bool IsTokenExpired
    {
        get
        {
            if (_token is null) return true;
            if (_token.ExpiresAt <= 0) return false;
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= _token.ExpiresAt;
        }
    }

    public bool HasRefreshToken => _token is not null && !string.IsNullOrEmpty(_token.RefreshToken);

    public bool IsRefreshTokenExpired
    {
        get
        {
            if (_token is null || string.IsNullOrEmpty(_token.RefreshToken)) return true;
            if (_token.RefreshTokenExpiresAt <= 0) return false;
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= _token.RefreshTokenExpiresAt;
        }
    }

    public async Task<Token> AuthenticateAsync(
        string otp = "",
        string transactionId = "",
        TimeSpan? pollInterval = null,
        int pollMaxRetries = 6,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(transactionId))
        {
            return await PollSmartOtpAsync(transactionId, pollInterval ?? TimeSpan.FromSeconds(5), pollMaxRetries, ct);
        }
        return await AuthenticateOnceAsync(otp: otp, ct: ct);
    }

    private async Task<Token> AuthenticateOnceAsync(string otp = "", string transactionId = "", CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret))
            throw new AuthenticationException("api_key and api_secret are required for authentication");

        var body = new Dictionary<string, object>
        {
            ["apiKey"] = _apiKey,
            ["apiSecret"] = _apiSecret,
        };
        if (!string.IsNullOrEmpty(otp))
            body["otp"] = otp;
        if (!string.IsNullOrEmpty(transactionId))
            body["transactionId"] = transactionId;

        var data = await _rest.PostAsync(Constants.EpAccessToken, body, ct: ct);
        if (ExtractPayload(data) is not { } payload)
            throw new ApiException("Push-approval is pending", "202", Constants.SmartOtpPendingStatus, ToDictionary(data));

        _token = Models.Token.FromJson(payload);
        if (string.IsNullOrEmpty(_token.AccessToken))
            throw new ApiException("Push-approval is pending", "202", Constants.SmartOtpPendingStatus, ToDictionary(data));

        _rest.SetAuthHeader(_token.AccessToken);
        Log.Info("Authentication successful");
        return _token;
    }

    public async Task<Token> RefreshAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret))
            throw new AuthenticationException("api_key and api_secret are required for token refresh");
        if (!HasRefreshToken)
            throw new AuthenticationException("No refresh token available — authenticate first");

        var body = new Dictionary<string, object>
        {
            ["refreshToken"] = _token!.RefreshToken,
        };

        var data = await _rest.PostAsync(Constants.EpRefreshToken, body, ct: ct);
        if (ExtractPayload(data) is not { } payload)
            throw new ApiException("Unexpected response format while refreshing token", responseBody: ToDictionary(data));

        _token = Models.Token.FromJson(payload);
        if (string.IsNullOrEmpty(_token.AccessToken))
            throw new ApiException("Refreshed token payload is missing access token", responseBody: ToDictionary(data));

        _rest.SetAuthHeader(_token.AccessToken);
        Log.Info("Token refreshed successfully");
        return _token;
    }

    public async Task<JsonElement> RequestOtpAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret))
            throw new AuthenticationException("api_key and api_secret are required for OTP request");

        var body = new Dictionary<string, object>
        {
            ["apiKey"] = _apiKey,
            ["apiSecret"] = _apiSecret,
        };

        return await _rest.PostAsync(Constants.EpRequestOtp, body, ct: ct);
    }

    public void SetToken(Token token)
    {
        _token = token;
        _rest.SetAuthHeader(token.AccessToken);
        Log.Info("Access token set manually");
    }

    public async Task<string> EnsureAuthenticatedAsync(
        string otp = "",
        string transactionId = "",
        TimeSpan? pollInterval = null,
        int pollMaxRetries = 6,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(otp) || !string.IsNullOrEmpty(transactionId))
        {
            await AuthenticateAsync(otp, transactionId, pollInterval, pollMaxRetries, ct);
        }
        else if (_token is null || IsTokenExpired)
        {
            if (HasRefreshToken)
            {
                await RefreshAsync(ct);
            }
            else
            {
                throw new AuthenticationException("OTP or Smart OTP transactionId is required to authenticate — no refresh token available");
            }
        }
        return AccessToken;
    }

    private static bool IsSmartOtpPending(Exception ex)
    {
        if (ex is SsiException ssiEx)
        {
            if (ssiEx.StatusCode == Constants.SmartOtpPendingStatus) return true;
            if (ssiEx.ResponseBody is not null)
            {
                if (ssiEx.ResponseBody.TryGetValue("code", out var codeElem) &&
                    (codeElem.ToString() == Constants.SmartOtpPendingCode.ToString() || codeElem.ToString() == "401114"))
                    return true;
                if (ssiEx.ResponseBody.TryGetValue("status", out var statusElem) && statusElem.ToString() == "202")
                    return true;
            }
        }
        return false;
    }

    private async Task<Token> PollSmartOtpAsync(string transactionId, TimeSpan interval, int maxRetries, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await AuthenticateOnceAsync(transactionId: transactionId, ct: ct);
            }
            catch (Exception ex)
            {
                if (!IsSmartOtpPending(ex))
                    throw;

                if (attempt >= maxRetries)
                    throw new AuthenticationException($"Smart OTP approval not confirmed after {maxRetries} attempts — please approve on your device.", innerException: ex);

                Log.Info($"[Smart OTP] Pending approval (attempt {attempt}/{maxRetries}), retrying in {interval.TotalSeconds}s...");
                await Task.Delay(interval, ct);
            }
        }
        throw new AuthenticationException("Smart OTP polling failed");
    }

    private static Dictionary<string, JsonElement>? ToDictionary(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object) return null;
        return data.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
    }

    private static JsonElement? ExtractPayload(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object) return null;
        if (data.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Object)
            return d;
        if (data.TryGetProperty("accessToken", out _))
            return data;
        return null;
    }
}

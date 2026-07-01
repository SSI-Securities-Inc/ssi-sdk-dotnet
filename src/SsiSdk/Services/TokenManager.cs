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

    public async Task<Token> AuthenticateAsync(string otp = "", CancellationToken ct = default)
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

        var data = await _rest.PostAsync(Constants.EpAccessToken, body, ct: ct);
        var payload = ExtractPayload(data)
            ?? throw new ApiException("Unexpected response format while authenticating");

        _token = Models.Token.FromJson(payload);
        if (string.IsNullOrEmpty(_token.AccessToken))
            throw new ApiException("Authentication payload is missing access token");

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
        var payload = ExtractPayload(data)
            ?? throw new ApiException("Unexpected response format while refreshing token");

        _token = Models.Token.FromJson(payload);
        if (string.IsNullOrEmpty(_token.AccessToken))
            throw new ApiException("Refreshed token payload is missing access token");

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

    public async Task<string> EnsureAuthenticatedAsync(string otp = "", CancellationToken ct = default)
    {
        if (_token is null || IsTokenExpired)
        {
            if (HasRefreshToken)
            {
                await RefreshAsync(ct);
            }
            else if (!string.IsNullOrEmpty(otp))
            {
                await AuthenticateAsync(otp, ct);
            }
            else
            {
                throw new AuthenticationException("OTP is required to authenticate — no refresh token available");
            }
        }
        return AccessToken;
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

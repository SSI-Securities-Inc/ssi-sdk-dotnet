using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Models;

public sealed class Token
{
    private const long MillisecondThreshold = 10_000_000_000;

    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";

    /// <summary>Access-token expiry, epoch seconds. 0 means unknown (treated as expired).</summary>
    public long ExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Refresh-token expiry, epoch seconds. 0 means unknown.</summary>
    public long RefreshTokenExpiresAt { get; set; }

    /// <summary>The earliest known expiry of the access and refresh tokens, or 0 when neither is known.</summary>
    public long EffectiveExpiresAt
    {
        get
        {
            var known = new[] { ExpiresAt, RefreshTokenExpiresAt }.Where(t => t > 0).ToList();
            return known.Count > 0 ? known.Min() : 0;
        }
    }

    /// <summary>A refresh token is present and not (about to be) expired.</summary>
    public bool CanRefresh => CanRefreshAt(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    internal bool CanRefreshAt(double now)
    {
        if (string.IsNullOrEmpty(RefreshToken)) return false;
        if (RefreshTokenExpiresAt <= 0) return true;
        return now < RefreshTokenExpiresAt - Constants.TokenExpirySkewMs / 1000.0;
    }

    internal static long ToSeconds(JsonElement? value)
    {
        var seconds = Converter.ToInt64(value);
        if (seconds >= MillisecondThreshold) seconds /= 1000;
        return Math.Max(seconds, 0);
    }

    internal static Token FromJson(JsonElement el)
    {
        return new Token
        {
            AccessToken = Converter.ToStr(Converter.GetProp(el, "accessToken")),
            TokenType = Converter.ToStr(Converter.GetProp(el, "tokenType")) is { Length: > 0 } tt ? tt : "Bearer",
            ExpiresAt = ToSeconds(Converter.GetProp(el, "expiresAt")),
            RefreshToken = Converter.ToStr(Converter.GetProp(el, "refreshToken")),
            RefreshTokenExpiresAt = ToSeconds(Converter.GetProp(el, "refreshExpiresAt")),
        };
    }

    public override string ToString() =>
        $"Token {{ TokenType = {TokenType}, ExpiresAt = {ExpiresAt}, RefreshTokenExpiresAt = {RefreshTokenExpiresAt}, AccessToken = ***, RefreshToken = *** }}";
}

public sealed class TokenRequest
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string? Otp { get; set; }
    public string? TransactionId { get; set; }

    public Dictionary<string, object> ToDictionary()
    {
        var d = new Dictionary<string, object> { ["apiKey"] = ApiKey, ["apiSecret"] = ApiSecret };
        if (Otp is not null) d["otp"] = Otp;
        if (TransactionId is not null) d["transactionId"] = TransactionId;
        return d;
    }

    public override string ToString() => $"TokenRequest {{ ApiKey = {Redact.Mask(ApiKey)}, ApiSecret = ***, Otp = ***, TransactionId = *** }}";
}

public sealed class OtpRequest
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;

    public Dictionary<string, object> ToDictionary() =>
        new() { ["apiKey"] = ApiKey, ["apiSecret"] = ApiSecret };

    public override string ToString() => $"OtpRequest {{ ApiKey = {Redact.Mask(ApiKey)}, ApiSecret = *** }}";
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;

    public Dictionary<string, object> ToDictionary() => new() { ["refreshToken"] = RefreshToken };

    public override string ToString() => "RefreshTokenRequest { RefreshToken = *** }";
}

public sealed class OtpResponse
{
    public string Message { get; set; } = string.Empty;

    /// <summary>Smart OTP transaction id to poll with, when the server issued one.</summary>
    public string? TransactionId { get; set; }

    internal static OtpResponse FromJson(JsonElement el)
    {
        var payload = Converter.GetProp(el, "data") is { ValueKind: JsonValueKind.Object } d ? d : el;
        var txn = Converter.ToStr(Converter.GetProp(payload, "transactionId"));
        var msg = Converter.ToStr(Converter.Pick(payload, "message", "msg"));
        return new OtpResponse { Message = msg, TransactionId = txn.Length > 0 ? txn : null };
    }

    public override string ToString() => $"OtpResponse {{ Message = {Message}, TransactionId = *** }}";
}

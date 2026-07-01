using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Models;

public sealed class Token
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public long ExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public long RefreshTokenExpiresAt { get; set; }

    internal static Token FromJson(JsonElement el)
    {
        return new Token
        {
            AccessToken = Converter.ToStr(Converter.GetProp(el, "accessToken")),
            TokenType = Converter.ToStr(Converter.GetProp(el, "tokenType")) is { Length: > 0 } tt ? tt : "Bearer",
            ExpiresAt = Converter.ToInt64(Converter.GetProp(el, "expiresAt")),
            RefreshToken = Converter.ToStr(Converter.GetProp(el, "refreshToken")),
            RefreshTokenExpiresAt = Converter.ToInt64(Converter.GetProp(el, "refreshExpiresAt")),
        };
    }
}

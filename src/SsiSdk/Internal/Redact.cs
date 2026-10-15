using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SsiSdk.Internal;

/// <summary>Masks secrets before anything is logged, put in an exception or printed.</summary>
internal static partial class Redact
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "apisecret", "api_secret", "apikey", "api_key", "otp", "pin", "token",
        "accesstoken", "access_token", "refreshtoken", "refresh_token", "authorization",
        "signature", "x-signature", "password", "privatekey", "private_key",
        "transactionid", "transaction_id", "accountno", "account_no", "account",
    };

    [GeneratedRegex(@"\b(bearer)\s+[A-Za-z0-9._~+/=-]+", RegexOptions.IgnoreCase)]
    private static partial Regex BearerRegex();

    public static bool IsSensitiveKey(string key) => SensitiveKeys.Contains(key);

    /// <summary>Short values become "***"; longer ones keep only the last 4 characters.</summary>
    public static string Mask(string? value)
    {
        if (value is null || value.Length <= 8) return "***";
        return "***" + value[^4..];
    }

    public static string MaskBearer(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
        return BearerRegex().Replace(text, m =>
        {
            var token = m.Value[(m.Groups[1].Length)..].Trim();
            return "Bearer " + Mask(token);
        });
    }

    public static JsonNode? RedactNode(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return null;
            case JsonObject obj:
            {
                var copy = new JsonObject();
                foreach (var (key, value) in obj)
                {
                    if (value is not null && IsSensitiveKey(key))
                        copy[key] = Mask(NodeText(value));
                    else
                        copy[key] = RedactNode(value);
                }
                return copy;
            }
            case JsonArray arr:
            {
                var copy = new JsonArray();
                foreach (var item in arr) copy.Add(RedactNode(item));
                return copy;
            }
            case JsonValue val:
                return val.TryGetValue<string>(out var s) ? JsonValue.Create(MaskBearer(s)) : JsonNode.Parse(val.ToJsonString());
            default:
                return node.DeepClone();
        }
    }

    public static Dictionary<string, JsonElement>? RedactBody(Dictionary<string, JsonElement>? body)
    {
        if (body is null) return null;
        var result = new Dictionary<string, JsonElement>(body.Count);
        foreach (var (key, value) in body)
        {
            var node = value.ValueKind == JsonValueKind.Null ? null : JsonNode.Parse(value.GetRawText());
            JsonNode? redacted = node is not null && IsSensitiveKey(key)
                ? JsonValue.Create(Mask(NodeText(node)))
                : RedactNode(node);
            result[key] = JsonSerializer.SerializeToElement(redacted);
        }
        return result;
    }

    /// <summary>Redacts a JSON text; non-JSON text only gets bearer tokens masked.</summary>
    public static string RedactText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
        try
        {
            var redacted = RedactNode(JsonNode.Parse(text));
            return redacted?.ToJsonString() ?? string.Empty;
        }
        catch (JsonException)
        {
            return MaskBearer(text);
        }
    }

    /// <summary>scheme://host[:port]/path; drops userinfo, query and fragment.</summary>
    public static string SafeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return "<invalid url>";
        var path = uri.AbsolutePath == "/" ? "" : uri.AbsolutePath;
        return $"{uri.Scheme}://{uri.Authority}{path}";
    }

    private static string NodeText(JsonNode node) =>
        node is JsonValue v && v.TryGetValue<string>(out var s) ? s : node.ToJsonString();
}

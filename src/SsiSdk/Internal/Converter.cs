using System.Globalization;
using System.Text.Json;

namespace SsiSdk.Internal;

internal static class Converter
{
    internal static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string ToStr(JsonElement? el)
    {
        if (el is null) return string.Empty;
        var e = el.Value;
        return e.ValueKind switch
        {
            JsonValueKind.String => e.GetString() ?? string.Empty,
            JsonValueKind.Number => e.GetDecimal() == Math.Truncate(e.GetDecimal())
                ? ((long)e.GetDecimal()).ToString(CultureInfo.InvariantCulture)
                : e.GetDecimal().ToString(CultureInfo.InvariantCulture),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            _ => e.GetRawText(),
        };
    }

    public static double ToFloat64(JsonElement? el)
    {
        if (el is null) return 0;
        var e = el.Value;
        return e.ValueKind switch
        {
            JsonValueKind.Number => e.GetDouble(),
            JsonValueKind.String => double.TryParse(e.GetString(), NumberStyles.Any,
                CultureInfo.InvariantCulture, out var v) ? v : 0,
            _ => 0,
        };
    }

    public static int ToInt(JsonElement? el)
    {
        if (el is null) return 0;
        var e = el.Value;
        return e.ValueKind switch
        {
            JsonValueKind.Number => (int)e.GetDouble(),
            JsonValueKind.String => int.TryParse(e.GetString(), out var v) ? v : 0,
            _ => 0,
        };
    }

    public static long ToInt64(JsonElement? el)
    {
        if (el is null) return 0;
        var e = el.Value;
        return e.ValueKind switch
        {
            JsonValueKind.Number => (long)e.GetDouble(),
            JsonValueKind.String => long.TryParse(e.GetString(), out var v) ? v : 0,
            _ => 0,
        };
    }

    public static JsonElement? GetProp(JsonElement el, string name)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;
        return el.TryGetProperty(name, out var prop) ? prop : null;
    }

    public static List<JsonElement> GetArray(JsonElement? el)
    {
        if (el is null || el.Value.ValueKind != JsonValueKind.Array) return [];
        var list = new List<JsonElement>();
        foreach (var item in el.Value.EnumerateArray())
            list.Add(item);
        return list;
    }
}

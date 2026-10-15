using System.Globalization;
using System.Text.Json;

namespace SsiSdk.Internal;

internal static class Converter
{
    internal static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly string[] DateFormats =
    [
        "yyyy/MM/dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy HH:mm:ss",
        "yyyy/MM/dd", "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy",
    ];

    public static string ToStr(JsonElement? el)
    {
        if (el is null) return string.Empty;
        var e = el.Value;
        return e.ValueKind switch
        {
            JsonValueKind.String => e.GetString() ?? string.Empty,
            JsonValueKind.Number => e.TryGetDecimal(out var d)
                ? FormatDecimal(d)
                : e.GetDouble().ToString("R", CultureInfo.InvariantCulture),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            _ => e.GetRawText(),
        };
    }

    /// <summary>Text of a value, or null when it is absent, null or blank.</summary>
    public static string? ToOptStr(JsonElement? el)
    {
        var s = ToStr(el);
        return s.Length == 0 ? null : s;
    }

    /// <summary>True for <c>true</c>, a non-zero number, or the text "true"/"1"/"yes"; everything else is false.</summary>
    public static bool ToBool(JsonElement? el)
    {
        if (el is null) return false;
        var e = el.Value;
        return e.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.Number => e.TryGetDecimal(out var d) && d != 0,
            JsonValueKind.String => e.GetString()?.Trim().ToLowerInvariant() is "true" or "1" or "yes",
            _ => false,
        };
    }

    /// <summary>The <c>data</c> object when the response wraps its payload, otherwise the response itself.</summary>
    public static JsonElement Payload(JsonElement el) =>
        GetProp(el, "data") is { ValueKind: JsonValueKind.Object } d ? d : el;

    /// <summary>Parses a number or numeric string (commas and spaces stripped). Bool, blank and non-finite give null.</summary>
    public static decimal? ToDecimalOrNull(JsonElement? el)
    {
        if (el is null) return null;
        var e = el.Value;
        switch (e.ValueKind)
        {
            case JsonValueKind.Number:
                if (e.TryGetDecimal(out var d)) return d;
                var dbl = e.GetDouble();
                return double.IsFinite(dbl) && Math.Abs(dbl) < (double)decimal.MaxValue ? (decimal)dbl : null;
            case JsonValueKind.String:
                return ParseDecimalText(e.GetString());
            default:
                return null;
        }
    }

    public static decimal? ParseDecimalText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var cleaned = text.Replace(",", "").Replace(" ", "").Trim();
        if (cleaned.Length == 0) return null;
        return decimal.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    /// <summary>Blank or unparseable gives 0.</summary>
    public static decimal ToDecimal(JsonElement? el) => ToDecimalOrNull(el) ?? 0m;

    public static double ToFloat64(JsonElement? el) => ToOptFloat(el) ?? 0;

    public static double? ToOptFloat(JsonElement? el)
    {
        if (el is null) return null;
        var e = el.Value;
        double v;
        switch (e.ValueKind)
        {
            case JsonValueKind.Number:
                v = e.GetDouble();
                break;
            case JsonValueKind.String:
                var text = e.GetString()?.Replace(",", "").Trim();
                if (string.IsNullOrEmpty(text)
                    || !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    return null;
                break;
            default:
                return null;
        }
        return double.IsFinite(v) ? v : null;
    }

    public static int ToInt(JsonElement? el)
    {
        var v = ToOptInt64(el) ?? 0;
        return (int)Math.Clamp(v, int.MinValue, int.MaxValue);
    }

    public static int? ToOptInt(JsonElement? el)
    {
        var v = ToOptInt64(el);
        return v is null ? null : (int)Math.Clamp(v.Value, int.MinValue, int.MaxValue);
    }

    public static long ToInt64(JsonElement? el) => ToOptInt64(el) ?? 0;

    /// <summary>Truncates; accepts "1234.0" and "2,493,089". Blank or invalid gives null.</summary>
    public static long? ToOptInt64(JsonElement? el)
    {
        var d = ToDecimalOrNull(el);
        if (d is null) return null;
        var t = Math.Truncate(d.Value);
        if (t > long.MaxValue || t < long.MinValue) return null;
        return (long)t;
    }

    public static JsonElement? GetProp(JsonElement el, string name)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;
        return el.TryGetProperty(name, out var prop) ? prop : null;
    }

    /// <summary>First property (by name, in order) whose value is present and not null.</summary>
    public static JsonElement? Pick(JsonElement el, params string[] names)
    {
        if (el.ValueKind != JsonValueKind.Object) return null;
        foreach (var name in names)
        {
            if (el.TryGetProperty(name, out var prop) && prop.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
                return prop;
        }
        return null;
    }

    public static List<JsonElement> GetArray(JsonElement? el)
    {
        if (el is null || el.Value.ValueKind != JsonValueKind.Array) return [];
        var list = new List<JsonElement>();
        foreach (var item in el.Value.EnumerateArray())
            list.Add(item);
        return list;
    }

    /// <summary>The server's "no content" marker: an object whose <c>code</c> is 204.</summary>
    public static bool IsNoContent(JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty("code", out var code)) return false;
        return ToStr(code) == "204";
    }

    /// <summary>Plain decimal text with trailing zeros removed and never an exponent.</summary>
    public static string FormatDecimal(decimal value) =>
        Normalize(value).ToString(CultureInfo.InvariantCulture);

    public static decimal Normalize(decimal value) => value / 1.0000000000000000000000000000m;

    /// <summary>Price as it goes on the wire: a plain decimal string.</summary>
    public static string FormatPrice(decimal price) => FormatDecimal(price);

    /// <summary>Parses a price given as text; bool-like or unparseable input is a ValidationException.</summary>
    public static decimal ParsePrice(string? text)
    {
        if (text is null) throw new ValidationException("price must be a number");
        var d = ParseDecimalText(text);
        if (d is null) throw new ValidationException($"price must be a number, got '{text}'");
        return d.Value;
    }

    public static DateTime? ParseDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        foreach (var fmt in DateFormats)
        {
            if (DateTime.TryParseExact(text.Trim(), fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
        }
        return null;
    }

    /// <summary>An int-like number when whole, otherwise a double.</summary>
    public static object ToNumber(decimal value) =>
        value == Math.Truncate(value) && value >= long.MinValue && value <= long.MaxValue
            ? (object)(long)value
            : (object)value;
}

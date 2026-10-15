using System.Globalization;
using System.Text.RegularExpressions;

namespace SsiSdk;

internal static partial class Validate
{
    private const string DateFormat = "yyyy/MM/dd";
    private const string DateTimeFormat = "yyyy/MM/dd HH:mm:ss";

    [GeneratedRegex(@"\A[A-Za-z0-9]+\z")]
    private static partial Regex SymbolRegex();

    /// <summary>Null, blank or whitespace-only text is rejected.</summary>
    public static void RequireNonEmpty(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException($"{fieldName} is required and cannot be empty");
    }

    /// <summary>Null or an empty collection is rejected.</summary>
    public static void RequireNonEmpty<T>(IEnumerable<T>? value, string fieldName)
    {
        if (value is null || !value.Any())
            throw new ValidationException($"{fieldName} is required and cannot be empty");
    }

    /// <summary>Only null is rejected: 0 and false count as present.</summary>
    public static void RequireNonEmpty<T>(T? value, string fieldName) where T : struct
    {
        if (value is null)
            throw new ValidationException($"{fieldName} is required and cannot be empty");
    }

    public static void RequirePositive(decimal value, string fieldName)
    {
        if (value <= 0)
            throw new ValidationException($"{fieldName} must be positive, got {value.ToString(CultureInfo.InvariantCulture)}");
    }

    public static void RequireNonNegative(decimal value, string fieldName)
    {
        if (value < 0)
            throw new ValidationException($"{fieldName} must be non-negative, got {value.ToString(CultureInfo.InvariantCulture)}");
    }

    public static void RequireEmpty(string? value, string fieldName)
    {
        if (!string.IsNullOrEmpty(value))
            throw new ValidationException($"{fieldName} must be empty or null, got '{value}'");
    }

    public static void RequireEmpty<T>(T? value, string fieldName) where T : struct
    {
        if (value is not null)
            throw new ValidationException($"{fieldName} must be empty or null, got '{value}'");
    }

    public static void RequireIn(string value, IReadOnlyCollection<string> allowed, string fieldName)
    {
        if (!allowed.Contains(value))
        {
            var allowedStr = string.Join(", ", allowed.OrderBy(x => x, StringComparer.Ordinal).Select(x => $"'{x}'"));
            throw new ValidationException($"{fieldName} must be one of [{allowedStr}], got '{value}'");
        }
    }

    /// <summary>A single alphanumeric ticker symbol (no lists, no separators).</summary>
    public static string RequireSymbol(string? value, string fieldName = "symbol")
    {
        RequireNonEmpty(value, fieldName);
        if (!SymbolRegex().IsMatch(value!))
            throw new ValidationException($"{fieldName} must be a single alphanumeric symbol, got '{value}'");
        return value!;
    }

    /// <summary>Parses a yyyy/MM/dd (optionally yyyy/MM/dd HH:mm:ss) request date.</summary>
    public static DateTime ParseDateArg(string? value, string fieldName, bool allowTime = false, bool timeRequired = false)
    {
        RequireNonEmpty(value, fieldName);
        string[] formats = timeRequired ? [DateTimeFormat]
            : allowTime ? [DateFormat, DateTimeFormat]
            : [DateFormat];
        foreach (var fmt in formats)
        {
            if (DateTime.TryParseExact(value, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
        }
        var expected = timeRequired ? "YYYY/MM/DD HH:MM:SS"
            : allowTime ? "YYYY/MM/DD HH:MM:SS or YYYY/MM/DD"
            : "YYYY/MM/DD";
        throw new ValidationException($"{fieldName} must be formatted {expected}, got '{value}'");
    }

    /// <summary>Both dates well formed, from &lt;= to, optional span cap in days.</summary>
    public static void RequireDateRange(string? from, string? to, bool allowTime = false, bool timeRequired = false, int? maxDays = null)
    {
        var start = ParseDateArg(from, "from", allowTime, timeRequired);
        var end = ParseDateArg(to, "to", allowTime, timeRequired);
        if (start > end)
            throw new ValidationException($"from must not be after to ({from} > {to})");
        if (maxDays is not null && end - start > TimeSpan.FromDays(maxDays.Value))
            throw new ValidationException($"date range must not exceed {maxDays} days");
    }

    public static void RequireExactlyOne(bool first, bool second, string firstName, string secondName)
    {
        if (first == second)
            throw new ValidationException($"exactly one of {firstName} or {secondName} is required");
    }
}

namespace SsiSdk;

internal static class Validate
{
    public static void RequireNonEmpty(string value, string fieldName)
    {
        if (string.IsNullOrEmpty(value))
            throw new ValidationException($"{fieldName} is required and cannot be empty");
    }

    public static void RequirePositive(double value, string fieldName)
    {
        if (value <= 0)
            throw new ValidationException($"{fieldName} must be positive, got {value}");
    }

    public static void RequireNonNegative(double value, string fieldName)
    {
        if (value < 0)
            throw new ValidationException($"{fieldName} must be non-negative, got {value}");
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

    public static void RequireIn(string value, HashSet<string> allowed, string fieldName)
    {
        if (!allowed.Contains(value))
        {
            var allowedStr = string.Join(", ", allowed.OrderBy(x => x).Select(x => $"'{x}'"));
            throw new ValidationException($"{fieldName} must be one of [{allowedStr}], got '{value}'");
        }
    }
}

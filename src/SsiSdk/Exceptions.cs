using System.Text.Json;

namespace SsiSdk;

public class SsiException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }
    public Dictionary<string, JsonElement>? ResponseBody { get; }

    public SsiException(string message, string code = "", int statusCode = 0,
        Dictionary<string, JsonElement>? responseBody = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}

public class AuthenticationException : SsiException
{
    public AuthenticationException(string message, string code = "", int statusCode = 0,
        Dictionary<string, JsonElement>? responseBody = null)
        : base(message, code, statusCode, responseBody) { }
}

public class ApiException : SsiException
{
    public ApiException(string message, string code = "", int statusCode = 0,
        Dictionary<string, JsonElement>? responseBody = null)
        : base(message, code, statusCode, responseBody) { }
}

public class WebSocketException : SsiException
{
    public WebSocketException(string message)
        : base(message) { }
}

public class ValidationException : SsiException
{
    public ValidationException(string message)
        : base(message) { }
}

public class RateLimitException : SsiException
{
    public double? RetryAfter { get; }

    public RateLimitException(string message, double? retryAfter = null)
        : base(message, "RATE_LIMITED")
    {
        RetryAfter = retryAfter;
    }
}

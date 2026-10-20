using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk;

public class SsiException : Exception
{
    /// <summary>Server error code (e.g. "401114") when the server sent one, otherwise the HTTP status or an SDK code.</summary>
    public string Code { get; }
    public int StatusCode { get; }

    /// <summary>Response body with secrets masked.</summary>
    public Dictionary<string, JsonElement>? ResponseBody { get; }

    /// <summary>Response headers (rate-limit headers, Retry-After, ...). Never includes Authorization.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    public SsiException(string message, string code = "", int statusCode = 0,
        Dictionary<string, JsonElement>? responseBody = null, Exception? innerException = null,
        IReadOnlyDictionary<string, string>? headers = null)
        : base(message, innerException)
    {
        Code = code;
        StatusCode = statusCode;
        ResponseBody = Redact.RedactBody(responseBody);
        Headers = headers ?? new Dictionary<string, string>();
    }
}

public class AuthenticationException : SsiException
{
    public AuthenticationException(string message, string code = "", int statusCode = 0,
        Dictionary<string, JsonElement>? responseBody = null, Exception? innerException = null,
        IReadOnlyDictionary<string, string>? headers = null)
        : base(message, code, statusCode, responseBody, innerException, headers) { }
}

/// <summary>The Smart OTP push is still waiting for the user to approve it. Call again with the same transaction id.</summary>
public class SmartOtpPendingException : AuthenticationException
{
    public SmartOtpPendingException(string message, string code = "401114", int statusCode = 202,
        Dictionary<string, JsonElement>? responseBody = null, IReadOnlyDictionary<string, string>? headers = null)
        : base(message, code, statusCode, responseBody, null, headers) { }
}

/// <summary>The Smart OTP push was rejected, not found, or belongs to another transaction. Terminal.</summary>
public class SmartOtpRejectedException : AuthenticationException
{
    public SmartOtpRejectedException(string message, string code = "", int statusCode = 0,
        Dictionary<string, JsonElement>? responseBody = null, IReadOnlyDictionary<string, string>? headers = null)
        : base(message, code, statusCode, responseBody, null, headers) { }
}

/// <summary>The refresh token is no longer usable; the caller must authenticate again with a fresh OTP.</summary>
public class ReauthenticationRequiredException : AuthenticationException
{
    public ReauthenticationRequiredException(string message, string code = "", int statusCode = 0,
        Dictionary<string, JsonElement>? responseBody = null, IReadOnlyDictionary<string, string>? headers = null)
        : base(message, code, statusCode, responseBody, null, headers) { }
}

public class ApiException : SsiException
{
    public ApiException(string message, string code = "", int statusCode = 0,
        Dictionary<string, JsonElement>? responseBody = null,
        IReadOnlyDictionary<string, string>? headers = null)
        : base(message, code, statusCode, responseBody, null, headers) { }
}

/// <summary>HTTP 409: the server already saw this client request id.</summary>
public class DuplicateRequestException : ApiException
{
    public DuplicateRequestException(string message, string code = "409", int statusCode = 409,
        Dictionary<string, JsonElement>? responseBody = null,
        IReadOnlyDictionary<string, string>? headers = null)
        : base(message, code, statusCode, responseBody, headers) { }
}

public class WebSocketException : SsiException
{
    public WebSocketException(string message, Exception? innerException = null, string code = "", int statusCode = 0,
        Dictionary<string, JsonElement>? responseBody = null,
        IReadOnlyDictionary<string, string>? headers = null)
        : base(message, code, statusCode, responseBody, innerException, headers) { }
}

/// <summary>
/// Trading WebSocket: the server refused a command or the socket itself. The message never carries the
/// signed params.
/// </summary>
public sealed class TradingWsError : WebSocketException
{
    /// <summary>HTTP-like status of the error frame (0 when it was not a number).</summary>
    public int Status { get; }

    /// <summary>Server error code, for example "401006".</summary>
    public string ErrorCode { get; }

    public string ServerMessage { get; }

    public TradingWsError(int status, string errorCode, string serverMessage)
        : base($"{status} {errorCode}: {serverMessage}", null, errorCode, status)
    {
        Status = status;
        ErrorCode = errorCode;
        ServerMessage = serverMessage;
    }
}

public class ValidationException : SsiException
{
    public ValidationException(string message)
        : base(message) { }
}

public class RateLimitException : SsiException
{
    /// <summary>Milliseconds the server asked us to wait (Retry-After), when sent.</summary>
    public long? RetryAfterMs { get; }
    public int? Limit { get; }
    public int? Remaining { get; }
    public double? Reset { get; }

    public RateLimitException(string message, long? retryAfterMs = null, string code = "RATE_LIMITED",
        int statusCode = 429, Dictionary<string, JsonElement>? responseBody = null,
        IReadOnlyDictionary<string, string>? headers = null,
        int? limit = null, int? remaining = null, double? reset = null)
        : base(message, code, statusCode, responseBody, null, headers)
    {
        RetryAfterMs = retryAfterMs;
        Limit = limit;
        Remaining = remaining;
        Reset = reset;
    }
}

/// <summary>Error codes the SSI FastConnect server returns in the <c>code</c> field.</summary>
public static class ServerErrorCode
{
    public const int CredentialNotLinkedToClient = 400101;
    public const int TwoFaInfoError = 400102;
    public const int TwoFaInfoMissing = 400103;
    public const int OtpSendFailed = 400104;
    public const int SymbolRequired = 400104;
    public const int PushApprovalCreateFailed = 400171;
    public const int OtpVerificationFailed = 401100;
    public const int RefreshTokenInvalid = 401101;
    public const int RefreshClientMissing = 401102;
    public const int RefreshTokenRevokedOrExpired = 401103;
    public const int RefreshApiKeyGone = 401104;
    public const int InvalidApiCredential = 401105;
    public const int TokenWithoutOtp = 401105;
    public const int PushApprovalNotFound = 401113;
    public const int PushApprovalPending = 401114;
    public const int SmartOtpPending = 401114;
    public const int PushApprovalRejected = 401115;
    public const int PushApprovalForeignTransaction = 401116;
    public const int NoScopeGranted = 403100;

    public const int SignatureMissing = 401006;
    public const int SignatureBodyEmpty = 401008;
    public const int SignatureMismatch = 401013;
    public const int SignatureInvalid = 401013;

    public const int ModifyPriceAndQuantity = 400107;
    public const int ReplayWindowExceeded = 400111;

    public const int SymbolInvalidCharacters = 400006;
    public const int FromAfterTo = 400202;
    public const int BoardNotAllowed = 400208;
    public const int TimeframeNotSupported = 400210;
    public const int DateFormatInvalid = 400213;
    public const int IndexSummaryNeedsOneOf = 504207;
    public const int SecuritiesInfoNeedsOneOf = 504208;
    public const int SecuritiesSummaryNeedsSymbolXorIndex = 504209;

    private static readonly HashSet<int> Known =
    [
        400101, 400102, 400103, 400104, 400171, 401100, 401101, 401102, 401103, 401104, 401105,
        401113, 401114, 401115, 401116, 403100, 401006, 401008, 401013, 400107, 400111,
        400006, 400202, 400208, 400210, 400213, 504207, 504208, 504209,
    ];

    public static bool IsKnown(int code) => Known.Contains(code);
}

/// <summary>Classification helpers for HTTP status codes.</summary>
public static class HttpStatus
{
    public static bool IsAuthError(int status) => status is 401 or 403;
    public static bool IsRateLimit(int status) => status == 429;
    public static bool IsClientError(int status) => status is >= 400 and < 500;
    public static bool IsServerError(int status) => status >= 500;
}

using System.Security.Cryptography;
using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk;

/// <summary>A success frame: <c>{"id","method","status":200,"result","rateLimit",...}</c>.</summary>
public sealed class TradingWsResponse
{
    public string Id { get; init; } = "";
    public string Method { get; init; } = "";
    public int Status { get; init; } = 200;

    /// <summary>The <c>result</c> member; Undefined when the frame had none.</summary>
    public JsonElement Result { get; init; }

    /// <summary>Quota the server reported with this frame (limit / remaining / resetMs).</summary>
    public IReadOnlyDictionary<string, JsonElement> RateLimit { get; init; } = new Dictionary<string, JsonElement>();

    public long? ReceivedTime { get; init; }

    internal static TradingWsResponse FromFrame(JsonElement frame)
    {
        var rate = new Dictionary<string, JsonElement>();
        if (frame.TryGetProperty("rateLimit", out var rl) && rl.ValueKind == JsonValueKind.Object)
            foreach (var p in rl.EnumerateObject()) rate[p.Name] = p.Value.Clone();
        long? received = null;
        if (frame.TryGetProperty("receivedTime", out var rt) && rt.ValueKind == JsonValueKind.Number && rt.TryGetInt64(out var r))
            received = r;
        return new TradingWsResponse
        {
            Id = StringOf(frame, "id"),
            Method = StringOf(frame, "method"),
            Status = StatusOf(frame, 200),
            Result = frame.TryGetProperty("result", out var res) ? res.Clone() : default,
            RateLimit = rate,
            ReceivedTime = received,
        };
    }

    internal static string StringOf(JsonElement frame, string name) =>
        frame.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null
            ? (v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.ToString())
            : "";

    internal static int StatusOf(JsonElement frame, int fallback)
    {
        if (!frame.TryGetProperty("status", out var s)) return fallback;
        if (s.ValueKind == JsonValueKind.Number && s.TryGetInt32(out var n)) return n;
        if (s.ValueKind == JsonValueKind.String && int.TryParse(s.GetString(), out var m)) return m;
        return 0;
    }
}

internal static class TradingWsProtocol
{
    /// <summary>Methods the server requires a signature for (matched case-insensitively).</summary>
    public static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "order.place", "order.amend", "order.cancel", "order.batchnew", "order.batchcancel", "fco.place", "fco.cancel",
    };

    public static readonly HashSet<string> SignatureCodes = ["401006", "401008", "401013"];

    public static bool IsMutating(string method) => MutatingMethods.Contains(method);

    /// <summary>Compact JSON of the params; this exact text is signed and sent.</summary>
    public static string ParamsText(object? parameters) => JsonSerializer.Serialize(parameters);

    public static string NewRequestId() => Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();

    /// <summary>One command frame: <c>{"id","method","params"[,"signature"],"timestamp"}</c>.</summary>
    public static string BuildCommand(
        string method, object? parameters, string? privateKey, string requestId, long timestampMs, string encoding = "base64")
    {
        if (string.IsNullOrEmpty(requestId))
            throw new WebSocketException("a command needs a non-empty id");
        var paramsText = ParamsText(parameters);
        var signaturePart = "";
        if (IsMutating(method))
        {
            if (string.IsNullOrEmpty(privateKey))
                throw new WebSocketException("private_key is required for trading commands that change state");
            signaturePart = ",\"signature\":" + JsonSerializer.Serialize(Crypto.Sign(paramsText, privateKey, encoding));
        }
        return "{\"id\":" + JsonSerializer.Serialize(requestId)
            + ",\"method\":" + JsonSerializer.Serialize(method)
            + ",\"params\":" + paramsText
            + signaturePart
            + ",\"timestamp\":" + timestampMs.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "}";
    }

    /// <summary>Returns the frame, or throws <see cref="TradingWsError"/> for an error frame.</summary>
    public static JsonElement Unwrap(JsonElement frame, string? encodingHint = null)
    {
        var hasError = frame.TryGetProperty("error", out var error)
            && error.ValueKind is not (JsonValueKind.Null or JsonValueKind.False or JsonValueKind.Undefined);
        var status = TradingWsResponse.StatusOf(frame, 200);
        if (!hasError && status < 400) return frame;

        var code = "";
        var message = "";
        if (hasError && error.ValueKind == JsonValueKind.Object)
        {
            code = TradingWsResponse.StringOf(error, "code");
            message = TradingWsResponse.StringOf(error, "message");
        }
        if (!string.IsNullOrEmpty(encodingHint) && SignatureCodes.Contains(code))
        {
            var other = encodingHint == "base64" ? "hex" : "base64";
            message += $" (signature rejected: signed as {encodingHint}; if every signed command fails "
                + $"this way set SignatureEncoding = \"{other}\" on the trading WS client)";
        }
        throw new TradingWsError(status, code, message);
    }

    public static TradingWsError BothRefused(TradingWsError error) =>
        new(error.Status, error.ErrorCode,
            $"{error.ServerMessage} (refused with a base64 and a hex signature: check that "
            + "Config.PrivateKey is the key registered for this API key)");

    /// <summary>A post-upgrade refusal: <c>{"code": 401|403|429|500, "msg": ...}</c> with no id.</summary>
    public static bool IsRejection(JsonElement frame) =>
        !frame.TryGetProperty("id", out _) && frame.TryGetProperty("code", out _)
        && (frame.TryGetProperty("msg", out _) || frame.TryGetProperty("message", out _));

    public static TradingWsError RejectionError(JsonElement frame)
    {
        var status = 0;
        if (frame.TryGetProperty("code", out var c))
        {
            if (c.ValueKind == JsonValueKind.Number && c.TryGetInt32(out var n)) status = n;
            else if (c.ValueKind == JsonValueKind.String && int.TryParse(c.GetString(), out var m)) status = m;
        }
        var message = TradingWsResponse.StringOf(frame, "msg");
        if (message.Length == 0) message = TradingWsResponse.StringOf(frame, "message");
        return new TradingWsError(status, TradingWsResponse.StringOf(frame, "code"), message);
    }

    /// <summary>The frame as an object, or null when it is not JSON (content is never logged).</summary>
    public static JsonElement? Decode(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.ValueKind == JsonValueKind.Object ? doc.RootElement.Clone() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

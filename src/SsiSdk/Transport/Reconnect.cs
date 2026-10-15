using System.Globalization;
using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Transport;

/// <summary>Why the server refused a connection it had already accepted.</summary>
internal enum RejectionKind
{
    /// <summary>401/403: the token is not (or no longer) valid.</summary>
    Auth,

    /// <summary>429: more than 10 sockets for this client.</summary>
    RateLimited,

    /// <summary>5xx and anything else: try again later.</summary>
    Transient,
}

/// <summary>Pure policy for WebSocket reconnection (no I/O).</summary>
internal static class Reconnect
{
    /// <summary>Exponential backoff with jitter in [0.5, 1.0] of the step, bounded by cap; never under 50 ms. Internal values are in seconds.</summary>
    public static double BackoffDelay(int attempt, double baseDelay, double cap = Constants.WsReconnectMaxDelayMs / 1000.0, double jitter = 0.5)
    {
        var exponential = Math.Min(cap, Math.Max(baseDelay, 0.0) * Math.Pow(2, Math.Max(attempt, 0)));
        return Math.Max(0.05, exponential * (0.5 + 0.5 * Math.Min(Math.Max(jitter, 0.0), 1.0)));
    }

    /// <summary>Classify the code/msg frame the server sends before it refuses a connection.</summary>
    public static (RejectionKind Kind, double? RetryAfter) ClassifyRejection(JsonElement? message)
    {
        var code = 0;
        double? retryAfter = null;
        if (message is { } m)
        {
            var text = Converter.ToStr(Converter.GetProp(m, "code")).Trim();
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out code)) code = 0;
            foreach (var key in new[] { "retryAfter", "retry_after" })
            {
                var v = Converter.ToOptFloat(Converter.GetProp(m, key));
                if (v is >= 0) { retryAfter = v; break; }
            }
        }
        if (code is 401 or 403) return (RejectionKind.Auth, retryAfter);
        if (code == 429) return (RejectionKind.RateLimited, retryAfter);
        return (RejectionKind.Transient, retryAfter);
    }

    /// <summary>Wait after a 429: the server's Retry-After if longer, never under the floor.</summary>
    public static double RateLimitWait(double? retryAfter) => Math.Max(Constants.WsRateLimitBackoffMs / 1000.0, retryAfter ?? 0.0);

    /// <summary>Seconds until the socket should be re-opened with a fresh token; null when the expiry is unknown.</summary>
    public static double? SecondsUntilRefresh(double? expiresAt, double now, double lead = Constants.WsTokenRefreshLeadMs / 1000.0)
    {
        if (expiresAt is null or <= 0) return null;
        return Math.Max(0.0, expiresAt.Value - lead - now);
    }
}

/// <summary>Remembers what was subscribed so it can be sent again after a reconnect.</summary>
internal sealed class SubscriptionBook
{
    private readonly object _lock = new();
    private readonly Dictionary<string, List<string>> _topics = new();

    public void Record(WsRequest request)
    {
        if (request.Channel is null || request.Topics is null) return;
        lock (_lock)
        {
            if (string.Equals(request.Method, StreamingMethod.Subscribe, StringComparison.OrdinalIgnoreCase))
            {
                if (!_topics.TryGetValue(request.Channel, out var book)) _topics[request.Channel] = book = [];
                foreach (var t in request.Topics)
                {
                    if (!book.Contains(t)) book.Add(t);
                }
            }
            else if (string.Equals(request.Method, StreamingMethod.Unsubscribe, StringComparison.OrdinalIgnoreCase))
            {
                if (!_topics.TryGetValue(request.Channel, out var book)) return;
                foreach (var t in request.Topics) book.Remove(t);
                if (book.Count == 0) _topics.Remove(request.Channel);
            }
        }
    }

    /// <summary>One subscribe request per channel that has topics, ready to send again.</summary>
    public List<WsRequest> Requests()
    {
        lock (_lock)
        {
            return _topics.Where(kv => kv.Value.Count > 0)
                .Select(kv => new WsRequest(StreamingMethod.Subscribe, kv.Key, kv.Value.ToList()))
                .ToList();
        }
    }

    public void Clear()
    {
        lock (_lock) _topics.Clear();
    }
}

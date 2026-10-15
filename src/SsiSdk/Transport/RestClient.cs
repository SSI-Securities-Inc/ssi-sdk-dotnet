using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Transport;

/// <summary>Rate-limit numbers the server sent on the last response (null when it sent none).</summary>
public sealed record RateLimitInfo(int? Limit, int? Remaining);

internal sealed class RestClient : IDisposable
{
    private static readonly Logger Log = new("ssi_sdk.transport.rest");

    private readonly Config _config;
    private readonly RateLimiter _rateLimiter;
    private readonly HttpClient _httpClient;
    private readonly object _lock = new();
    private string _authHeader = string.Empty;

    public RestClient(Config config, HttpMessageHandler? handler = null)
    {
        _config = config;
        _rateLimiter = new RateLimiter(config.RateLimitPerSecond);
        Logger.SetLevelFromString(config.LogLevel);

        if (handler is null)
        {
            var h = new HttpClientHandler();
            if (!string.IsNullOrEmpty(config.Proxy))
                h.Proxy = new WebProxy(config.Proxy);
            handler = h;
        }

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(config.ApiUrl),
            Timeout = TimeSpan.FromMilliseconds(config.TimeoutMs),
        };
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(Constants.HeaderAccept, Constants.ContentTypeJson);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(Constants.HeaderUserAgent,
            string.IsNullOrWhiteSpace(config.UserAgent) ? Constants.DefaultUserAgent : config.UserAgent);
    }

    public Config Config => _config;

    /// <summary>Limit and remaining quota from the last response.</summary>
    public RateLimitInfo RateLimit { get; private set; } = new(null, null);

    public string GetPrivateKey() => _config.PrivateKey;

    public void SetAuthHeader(string token)
    {
        lock (_lock)
        {
            _authHeader = Constants.AuthSchemeBearer + token;
        }
    }

    public void ClearAuthHeader()
    {
        lock (_lock)
        {
            _authHeader = string.Empty;
        }
    }

    /// <summary>
    /// Sends a request. Only GET is replayed on timeout (override with <paramref name="retry"/>);
    /// 429 is waited out for replayable requests only.
    /// </summary>
    public async Task<JsonElement> RequestAsync(
        HttpMethod method, string path,
        IEnumerable<KeyValuePair<string, string>>? queryParams = null,
        object? jsonBody = null,
        Dictionary<string, string>? headers = null,
        CancellationToken ct = default,
        bool? retry = null)
    {
        var url = Route(path);
        var replayable = retry ?? method == HttpMethod.Get;
        Log.Debug($"[{method}] {path}");

        var rateAttempt = 0;
        while (true)
        {
            await _rateLimiter.AcquireAsync(ct).ConfigureAwait(false);
            try
            {
                return replayable
                    ? await WithTimeoutRetryAsync(url, method, path, queryParams, jsonBody, headers, ct).ConfigureAwait(false)
                    : await DoRequestAsync(url, method, path, queryParams, jsonBody, headers, ct).ConfigureAwait(false);
            }
            catch (RateLimitException ex)
            {
                var wait = RateWait(ex, rateAttempt);
                if (!replayable || wait is null || rateAttempt >= Constants.RateLimitMaxRetries)
                    throw;
                Log.Info($"429 on {path}: waiting {wait} ms (attempt {rateAttempt + 1})");
                _rateLimiter.Pause(wait.Value / 1000.0);
                rateAttempt++;
            }
        }
    }

    private async Task<JsonElement> WithTimeoutRetryAsync(
        string url, HttpMethod method, string path,
        IEnumerable<KeyValuePair<string, string>>? queryParams, object? jsonBody,
        Dictionary<string, string>? headers, CancellationToken ct)
    {
        var maxRetries = Math.Max(0, _config.MaxRetries);
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await DoRequestAsync(url, method, path, queryParams, jsonBody, headers, ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                if (attempt >= maxRetries) throw;
                var delayMs = _config.RetryDelayMs * Math.Pow(2, attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), ct).ConfigureAwait(false);
            }
        }
    }

    private async Task<JsonElement> DoRequestAsync(
        string url, HttpMethod method, string path,
        IEnumerable<KeyValuePair<string, string>>? queryParams,
        object? jsonBody,
        Dictionary<string, string>? headers,
        CancellationToken ct)
    {
        if (queryParams is not null)
        {
            var qs = string.Join("&", queryParams
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
            if (qs.Length > 0) url += "?" + qs;
        }

        using var request = new HttpRequestMessage(method, url);

        if (jsonBody is not null)
        {
            var json = jsonBody is string rawJson ? rawJson : JsonSerializer.Serialize(jsonBody);
            request.Content = new StringContent(json, Encoding.UTF8, Constants.ContentTypeJson);
        }

        lock (_lock)
        {
            if (!string.IsNullOrEmpty(_authHeader))
                request.Headers.TryAddWithoutValidation(Constants.HeaderAuthorization, _authHeader);
        }

        if (headers is not null)
        {
            foreach (var (k, v) in headers)
                request.Headers.TryAddWithoutValidation(k, v);
        }

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        RateLimit = ObserveRateLimit(response);
        return await HandleResponseAsync(response, ct).ConfigureAwait(false);
    }

    private string Route(string path)
    {
        if (Constants.TradingApiPathPrefixes.Any(p => path.StartsWith(p, StringComparison.Ordinal)))
        {
            var baseUrl = _config.TradingApiUrl;
            if (baseUrl != _config.ApiUrl.TrimEnd('/'))
                return baseUrl + path;
        }
        return path;
    }

    private RateLimitInfo ObserveRateLimit(HttpResponseMessage response)
    {
        var limit = IntHeader(response, Constants.HeaderRateLimitLimit);
        var remaining = IntHeader(response, Constants.HeaderRateLimitRemaining);
        if (limit is not null) _rateLimiter.Cap(limit.Value);
        if (remaining == 0) _rateLimiter.Pause(1.0);
        return new RateLimitInfo(limit, remaining);
    }

    private static long? RateWait(RateLimitException error, int attempt)
    {
        var wait = error.RetryAfterMs is > 0
            ? error.RetryAfterMs.Value
            : Constants.RateLimitDefaultWaitMs * (1L << attempt);
        return wait <= Constants.RateLimitMaxWaitMs ? wait : null;
    }

    private static async Task<JsonElement> HandleResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var status = (int)response.StatusCode;
        var body = TryParse(text);

        if (status >= 400)
            throw ErrorFor(response, status, text, body);
        if (status == 204)
            return EmptyObject();

        if (status == Constants.SmartOtpPendingStatus && ServerCode(body) is int pendingCode && pendingCode == Constants.SmartOtpPendingCode)
            throw new SmartOtpPendingException(
                $"Smart OTP approval pending [{Constants.SmartOtpPendingCode}]: {ServerMessage(body, string.Empty)}",
                Constants.SmartOtpPendingCode.ToString(CultureInfo.InvariantCulture), status,
                ToDictionary(body), HeadersOf(response));

        if (body is null || body.Value.ValueKind == JsonValueKind.Null)
            throw new ApiException($"API returned a non-JSON body (HTTP {status})", status.ToString(CultureInfo.InvariantCulture),
                status, null, HeadersOf(response));

        var root = body.Value;
        if (root.ValueKind == JsonValueKind.Object
            && (root.TryGetProperty("msg", out _) || root.TryGetProperty("message", out _))
            && root.TryGetProperty("code", out var codeEl)
            && IntOrNull(codeEl) is int code && code is not (200 or 204))
            throw ErrorFor(response, status, text, root);

        return root;
    }

    private static SsiException ErrorFor(HttpResponseMessage response, int status, string text, JsonElement? body)
    {
        var code = ServerCode(body);
        var detail = ServerMessage(body, text.Length > 200 ? text[..200] : text);
        var message = $"API error {status}" + (code is not null ? $" [{code}]" : "");
        if (detail.Length > 0) message = $"{message}: {detail}";

        var shown = code?.ToString() ?? status.ToString(CultureInfo.InvariantCulture);
        var dict = ToDictionary(body);
        var headers = HeadersOf(response);

        if (status == Constants.HttpStatusTooManyRequests)
            return new RateLimitException(message, RetryAfterMs(response), shown, status, dict, headers,
                IntHeader(response, Constants.HeaderRateLimitLimit),
                IntHeader(response, Constants.HeaderRateLimitRemaining));
        if (status is 401 or 403)
        {
            if (code is int c && Constants.SmartOtpTerminalCodes.Contains(c))
                return new SmartOtpRejectedException(message, shown, status, dict, headers);
            return new AuthenticationException(message, shown, status, dict, null, headers);
        }
        if (status == Constants.HttpStatusConflict)
            return new DuplicateRequestException(message, shown, status, dict, headers);
        return new ApiException(message, shown, status, dict, headers);
    }

    /// <summary>The server's <c>code</c>: an int when numeric, otherwise its text; null when absent.</summary>
    private static object? ServerCode(JsonElement? body)
    {
        if (body is not { ValueKind: JsonValueKind.Object } b || !b.TryGetProperty("code", out var code)
            || code.ValueKind == JsonValueKind.Null)
            return null;
        if (IntOrNull(code) is int i) return i;
        return Converter.ToStr(code);
    }

    private static int? IntOrNull(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n)) return n;
        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString()?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s)) return s;
        return null;
    }

    private static string ServerMessage(JsonElement? body, string fallback)
    {
        if (body is { ValueKind: JsonValueKind.Object } b)
        {
            foreach (var key in new[] { "msg", "message" })
            {
                if (b.TryGetProperty(key, out var v))
                {
                    var s = Converter.ToStr(v);
                    if (s.Length > 0) return s;
                }
            }
        }
        return fallback;
    }

    private static int? IntHeader(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues(name, out var vals)) return null;
        return int.TryParse(vals.FirstOrDefault()?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    /// <summary>Retry-After header (seconds on the wire) as milliseconds.</summary>
    private static long? RetryAfterMs(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues(Constants.HeaderRetryAfter, out var vals)) return null;
        return double.TryParse(vals.FirstOrDefault()?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            && v >= 0 && v < long.MaxValue / 1000.0
            ? (long)Math.Round(v * 1000.0) : null;
    }

    private static Dictionary<string, string> HeadersOf(HttpResponseMessage response)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in response.Headers.Concat(response.Content.Headers))
        {
            if (h.Key.Equals("authorization", StringComparison.OrdinalIgnoreCase)
                || h.Key.Equals("set-cookie", StringComparison.OrdinalIgnoreCase))
                continue;
            result[h.Key] = string.Join(", ", h.Value);
        }
        return result;
    }

    private static JsonElement? TryParse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        try
        {
            using var doc = JsonDocument.Parse(text);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Dictionary<string, JsonElement>? ToDictionary(JsonElement? body)
    {
        if (body is not { ValueKind: JsonValueKind.Object } b) return null;
        return b.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
    }

    private static JsonElement EmptyObject()
    {
        using var doc = JsonDocument.Parse("{}");
        return doc.RootElement.Clone();
    }

    public Task<JsonElement> GetAsync(string path, IEnumerable<KeyValuePair<string, string>>? queryParams = null,
        Dictionary<string, string>? headers = null, CancellationToken ct = default) =>
        RequestAsync(HttpMethod.Get, path, queryParams, null, headers, ct);

    public Task<JsonElement> PostAsync(string path, object? jsonBody = null,
        Dictionary<string, string>? headers = null, CancellationToken ct = default, bool? retry = null) =>
        RequestAsync(HttpMethod.Post, path, null, jsonBody, headers, ct, retry);

    public Task<JsonElement> PutAsync(string path, object? jsonBody = null,
        Dictionary<string, string>? headers = null, CancellationToken ct = default, bool? retry = null) =>
        RequestAsync(HttpMethod.Put, path, null, jsonBody, headers, ct, retry);

    public Task<JsonElement> DeleteAsync(string path, object? jsonBody = null,
        Dictionary<string, string>? headers = null, CancellationToken ct = default, bool? retry = null) =>
        RequestAsync(HttpMethod.Delete, path, null, jsonBody, headers, ct, retry);

    public void Dispose() => _httpClient.Dispose();
}

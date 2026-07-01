using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Transport;

internal sealed class RestClient : IDisposable
{
    private static readonly Logger Log = new("ssi_sdk.transport.rest");

    private readonly Config _config;
    private readonly RateLimiter _rateLimiter;
    private readonly HttpClient _httpClient;
    private readonly object _lock = new();
    private string _authHeader = string.Empty;

    public RestClient(Config config)
    {
        _config = config;
        _rateLimiter = new RateLimiter(config.RateLimitPerSecond);
        Logger.SetLevelFromString(config.LogLevel);

        var handler = new HttpClientHandler();
        if (!string.IsNullOrEmpty(config.Proxy))
            handler.Proxy = new WebProxy(config.Proxy);

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(config.ApiUrl),
            Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds),
        };
        _httpClient.DefaultRequestHeaders.Add(Constants.HeaderAccept, Constants.ContentTypeJson);
    }

    public string GetPrivateKey() => _config.PrivateKey;

    public void SetAuthHeader(string token)
    {
        lock (_lock)
        {
            _authHeader = Constants.AuthSchemeBearer + token;
        }
    }

    public async Task<JsonElement> RequestAsync(
        HttpMethod method, string path,
        Dictionary<string, string>? queryParams = null,
        object? jsonBody = null,
        Dictionary<string, string>? headers = null,
        CancellationToken ct = default)
    {
        await _rateLimiter.AcquireAsync(ct);

        Exception? lastErr = null;
        for (var attempt = 0; attempt <= _config.MaxRetries; attempt++)
        {
            try
            {
                var result = await DoRequestAsync(method, path, queryParams, jsonBody, headers, ct);
                return result;
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                lastErr = ex;
                if (attempt < _config.MaxRetries)
                {
                    var delay = _config.RetryDelay * Math.Pow(2, attempt);
                    await Task.Delay(TimeSpan.FromSeconds(delay), ct);
                }
            }
            catch (SsiException)
            {
                throw;
            }
        }

        throw lastErr ?? new ApiException("Request failed after retries");
    }

    private async Task<JsonElement> DoRequestAsync(
        HttpMethod method, string path,
        Dictionary<string, string>? queryParams,
        object? jsonBody,
        Dictionary<string, string>? headers,
        CancellationToken ct)
    {
        var url = path;
        if (queryParams is { Count: > 0 })
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
            Log.Info($"[DEBUG] body_sent: {json}");
            Log.Info($"[DEBUG] body_is_raw: {jsonBody is string}");
            request.Content = new StringContent(json, Encoding.UTF8, Constants.ContentTypeJson);
        }
        else
        {
            Log.Debug($"[{method}] {path} | params: {queryParams?.Count ?? 0}");
        }

        lock (_lock)
        {
            if (!string.IsNullOrEmpty(_authHeader) && path != Constants.EpRefreshToken)
                request.Headers.TryAddWithoutValidation(Constants.HeaderAuthorization, _authHeader);
        }

        if (headers is not null)
        {
            foreach (var (k, v) in headers)
                request.Headers.TryAddWithoutValidation(k, v);
        }

        var response = await _httpClient.SendAsync(request, ct);
        return await HandleResponseAsync(response, ct);
    }

    private static async Task<JsonElement> HandleResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        var status = (int)response.StatusCode;

        Log.Debug($"<-- Status: {status} | Body: {body}");

        if (status == 401 || status == 403)
        {
            var rb = TryParseBody(body);
            throw new AuthenticationException($"Authentication failed: {status} | {body}", status.ToString(), status, rb);
        }

        if (status == 429)
        {
            double? retryAfter = null;
            if (response.Headers.TryGetValues(Constants.HeaderRetryAfter, out var vals))
            {
                var ra = vals.FirstOrDefault();
                if (ra is not null && double.TryParse(ra, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                    retryAfter = v;
            }
            throw new RateLimitException("Rate limit exceeded", retryAfter);
        }

        if (status >= 400)
        {
            var rb = TryParseBody(body);
            throw new ApiException($"API error: {status}", status.ToString(), status, rb);
        }

        if (status == 204)
            return JsonDocument.Parse("{}").RootElement.Clone();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
        {
            using var wrapped = JsonDocument.Parse("{\"data\":" + body + "}");
            return wrapped.RootElement.Clone();
        }
        return root.Clone();
    }

    private static Dictionary<string, JsonElement>? TryParseBody(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value.Clone());
        }
        catch
        {
            return null;
        }
    }

    public Task<JsonElement> GetAsync(string path, Dictionary<string, string>? queryParams = null,
        Dictionary<string, string>? headers = null, CancellationToken ct = default) =>
        RequestAsync(HttpMethod.Get, path, queryParams, null, headers, ct);

    public Task<JsonElement> PostAsync(string path, object? jsonBody = null,
        Dictionary<string, string>? headers = null, CancellationToken ct = default) =>
        RequestAsync(HttpMethod.Post, path, null, jsonBody, headers, ct);

    public Task<JsonElement> PutAsync(string path, object? jsonBody = null,
        Dictionary<string, string>? headers = null, CancellationToken ct = default) =>
        RequestAsync(HttpMethod.Put, path, null, jsonBody, headers, ct);

    public Task<JsonElement> DeleteAsync(string path, object? jsonBody = null,
        Dictionary<string, string>? headers = null, CancellationToken ct = default) =>
        RequestAsync(HttpMethod.Delete, path, null, jsonBody, headers, ct);

    public void Dispose() => _httpClient.Dispose();
}

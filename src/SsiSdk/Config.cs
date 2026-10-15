namespace SsiSdk;

public sealed class Config
{
    private string _apiUrl = Constants.DefaultApiUrl;
    private string _streamingUrl = Constants.DefaultStreamingUrl;
    private string _tradingApiDomain = Constants.DefaultApiUrl;
    private string _tradingWsDomain = Constants.DefaultTradingWsUrl;
    private int _maxBatchOrders = Constants.MaxBatchOrders;

    public string ClientId { get; set; } = string.Empty;

    /// <summary>REST base URL. Blank falls back to the default.</summary>
    public string ApiUrl
    {
        get => _apiUrl;
        set => _apiUrl = string.IsNullOrWhiteSpace(value) ? Constants.DefaultApiUrl : value.Trim();
    }

    /// <summary>Market-data streaming URL. Blank falls back to the default.</summary>
    public string StreamingUrl
    {
        get => _streamingUrl;
        set => _streamingUrl = string.IsNullOrWhiteSpace(value) ? Constants.DefaultStreamingUrl : value.Trim();
    }

    /// <summary>Host (or full URL) of the trading REST API. Blank falls back to the default.</summary>
    public string TradingApiDomain
    {
        get => _tradingApiDomain;
        set => _tradingApiDomain = string.IsNullOrWhiteSpace(value) ? Constants.DefaultApiUrl : value.Trim();
    }

    /// <summary>Host (or full URL) of the trading WebSocket. Blank falls back to the default.</summary>
    public string TradingWsDomain
    {
        get => _tradingWsDomain;
        set => _tradingWsDomain = string.IsNullOrWhiteSpace(value) ? Constants.DefaultTradingWsUrl : value.Trim();
    }

    /// <summary>Resolved base URL (scheme://host[:port]) that trading and account REST paths are sent to.</summary>
    public string TradingApiUrl
    {
        get
        {
            var raw = _tradingApiDomain.Trim().TrimEnd('/');
            if (raw.Length == 0 || string.Equals(raw, Constants.DefaultApiUrl, StringComparison.OrdinalIgnoreCase))
                return ApiUrl.TrimEnd('/');
            if (!raw.Contains("://", StringComparison.Ordinal))
                raw = "https://" + raw;
            if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || string.IsNullOrEmpty(uri.Host))
                throw new ValidationException("TradingApiDomain must be an http(s) URL or a host name");
            return uri.GetLeftPart(UriPartial.Authority);
        }
    }

    /// <summary>Resolved WebSocket URL of the trading channel (the /ws/v3/trading path is added when missing).</summary>
    public string TradingWsUrl
    {
        get
        {
            var raw = _tradingWsDomain.Trim().TrimEnd('/');
            if (raw.Length == 0) return Constants.DefaultTradingWsUrl;
            if (!raw.Contains("://", StringComparison.Ordinal))
                raw = "wss://" + raw;
            var schemeEnd = raw.IndexOf("://", StringComparison.Ordinal) + 3;
            if (raw.IndexOf('/', schemeEnd) < 0)
                raw += Constants.DefaultTradingWsPath;
            return raw;
        }
    }

    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    /// <summary>HTTP/WebSocket request timeout in milliseconds.</summary>
    public int TimeoutMs { get; set; } = Constants.DefaultTimeoutMs;
    public int MaxRetries { get; set; } = Constants.DefaultMaxRetries;

    /// <summary>Base delay in milliseconds for retry and reconnect backoff.</summary>
    public int RetryDelayMs { get; set; } = Constants.DefaultRetryDelayMs;
    public int RateLimitPerSecond { get; set; } = Constants.DefaultRateLimitPerSecond;
    public string LogLevel { get; set; } = "INFO";
    public string? Proxy { get; set; }

    /// <summary>User-Agent sent on REST and WebSocket requests. Null/blank uses the SDK default.</summary>
    public string? UserAgent { get; set; }

    /// <summary>Reconnect streaming sockets automatically (default true).</summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>Milliseconds between Smart OTP approval polls.</summary>
    public int OtpPollIntervalMs { get; set; } = Constants.DefaultOtpPollIntervalMs;

    /// <summary>Total milliseconds to wait for a Smart OTP approval before giving up.</summary>
    public int OtpPollMaxWaitMs { get; set; } = Constants.DefaultOtpPollMaxWaitMs;

    /// <summary>Maximum orders per batch request. Values below 1 reset to 20.</summary>
    public int MaxBatchOrders
    {
        get => _maxBatchOrders;
        set => _maxBatchOrders = value < 1 ? Constants.MaxBatchOrders : value;
    }

    public Config() { }

    public Config(string clientId) => ClientId = clientId;

    public override string ToString() =>
        $"Config {{ ClientId = {ClientId}, ApiUrl = {ApiUrl}, StreamingUrl = {StreamingUrl}, " +
        $"TradingApiDomain = {TradingApiDomain}, TradingWsDomain = {TradingWsDomain}, " +
        $"ApiKey = {Internal.Redact.Mask(ApiKey)}, ApiSecret = ***, PrivateKey = ***, " +
        $"TimeoutMs = {TimeoutMs}, MaxRetries = {MaxRetries}, AutoReconnect = {AutoReconnect} }}";
}

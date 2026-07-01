namespace SsiSdk;

public sealed class Config
{
    public string ClientId { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = Constants.DefaultApiUrl;
    public string StreamingUrl { get; set; } = Constants.DefaultStreamingUrl;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = Constants.DefaultTimeout;
    public int MaxRetries { get; set; } = Constants.DefaultMaxRetries;
    public double RetryDelay { get; set; } = Constants.DefaultRetryDelay;
    public int RateLimitPerSecond { get; set; } = Constants.DefaultRateLimitPerSecond;
    public string LogLevel { get; set; } = "INFO";
    public string? Proxy { get; set; }

    public Config() { }

    public Config(string clientId) => ClientId = clientId;
}

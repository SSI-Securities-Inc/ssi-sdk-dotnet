using SsiSdk.Models;
using SsiSdk.Services;
using SsiSdk.Transport;

namespace SsiSdk;

/// <summary>
/// Auth manages credentials and access tokens.
/// Entry point for all other specialized clients.
/// </summary>
public sealed class AuthClient : IDisposable
{
    private readonly RestClient _restClient;
    public TokenManager TokenManager { get; }

    internal Config Config { get; }
    internal RestClient RestClient => _restClient;

    public AuthClient(Config config)
    {
        Config = config;
        _restClient = new RestClient(config);
        TokenManager = new TokenManager(_restClient, config.ApiKey, config.ApiSecret);
    }

    public Task<Token> AuthenticateAsync(string otp = "", CancellationToken ct = default) =>
        TokenManager.AuthenticateAsync(otp, ct);

    public Task<Token> RefreshAsync(CancellationToken ct = default) =>
        TokenManager.RefreshAsync(ct);

    public Task<string> EnsureAuthenticatedAsync(string otp = "", CancellationToken ct = default) =>
        TokenManager.EnsureAuthenticatedAsync(otp, ct);

    public Task RequestOtpAsync(CancellationToken ct = default) =>
        TokenManager.RequestOtpAsync(ct);

    public string AccessToken => TokenManager.AccessToken;

    public void Dispose() => _restClient.Dispose();
}

/// <summary>
/// Data is the market data client.
/// Requires auth.AuthenticateAsync("") (no OTP) before use.
/// </summary>
public sealed class DataClient
{
    public MarketDataService MarketData { get; }

    public DataClient(AuthClient auth)
    {
        MarketData = new MarketDataService(auth.RestClient);
    }
}

/// <summary>
/// Trading is the client for orders, portfolio, and account management.
/// Requires auth.AuthenticateAsync(otp) with a valid OTP before use.
/// </summary>
public sealed class TradingClient
{
    public TradingService Trading { get; }
    public AccountService Account { get; }
    public PortfolioService Portfolio { get; }

    public TradingClient(AuthClient auth)
    {
        Trading = new TradingService(auth.RestClient, auth.Config.PrivateKey);
        Account = new AccountService(auth.RestClient);
        Portfolio = new PortfolioService(auth.RestClient, auth.Config.ClientId);
    }
}

/// <summary>
/// Stream is the real-time WebSocket client.
/// Requires auth.AuthenticateAsync(otp) with a valid OTP before use.
/// </summary>
public sealed class StreamClient : IDisposable
{
    private readonly WebSocketClient _wsClient;
    public StreamingService Streaming { get; }

    public StreamClient(AuthClient auth)
    {
        _wsClient = new WebSocketClient(auth.Config);
        var token = auth.AccessToken;
        if (!string.IsNullOrEmpty(token))
            _wsClient.SetToken(token);
        Streaming = new StreamingService(_wsClient);
    }

    public Task ConnectAsync(CancellationToken ct = default) => _wsClient.ConnectAsync(ct);
    public void Disconnect()
    {
        Streaming.StopPingLoop();
        _wsClient.Disconnect();
    }
    public bool IsConnected => _wsClient.IsConnected;
    public Task WaitAsync(TimeSpan? timeout = null) => _wsClient.WaitAsync(timeout);

    public void Dispose()
    {
        Disconnect();
    }
}

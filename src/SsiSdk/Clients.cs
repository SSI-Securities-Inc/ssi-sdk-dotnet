using System.Text.Json;
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
        TokenManager = new TokenManager(_restClient, config);
    }

    /// <summary>
    /// One call to the token endpoint. Pass at most one of <paramref name="otp"/> or <paramref name="transactionId"/>.
    /// For Smart OTP, a pending approval raises <see cref="SmartOtpPendingException"/>; use
    /// <see cref="EnsureAuthenticatedAsync"/> to poll.
    /// </summary>
    public Task<Token> AuthenticateAsync(string? otp = null, string? transactionId = null, CancellationToken ct = default) =>
        TokenManager.AuthenticateAsync(otp, transactionId, ct);

    public Task<Token> RefreshAsync(CancellationToken ct = default) =>
        TokenManager.RefreshAsync(ct);

    /// <summary>
    /// A valid access token: the current one, a refresh, an OTP login, or Smart OTP polling (in that order).
    /// Polling defaults to <c>Config.OtpPollIntervalMs</c> / <c>Config.OtpPollMaxWaitMs</c>; <paramref name="pollIntervalMs"/> is in milliseconds.
    /// </summary>
    public Task<string> EnsureAuthenticatedAsync(
        string? otp = null,
        string? transactionId = null,
        int? pollIntervalMs = null,
        int? pollMaxRetries = null,
        CancellationToken ct = default) =>
        TokenManager.EnsureAuthenticatedAsync(otp, transactionId, pollIntervalMs, pollMaxRetries, ct);

    public Task<JsonElement> RequestOtpAsync(CancellationToken ct = default) =>
        TokenManager.RequestOtpAsync(ct);

    /// <summary>Like <see cref="RequestOtpAsync"/>, typed: the message and the Smart OTP transaction id.</summary>
    public Task<OtpResponse> RequestOtpTypedAsync(CancellationToken ct = default) =>
        TokenManager.RequestOtpTypedAsync(ct);

    /// <summary>Use a token obtained elsewhere (for example restored from storage).</summary>
    public void SetToken(Token token) => TokenManager.SetToken(token);

    public string AccessToken => TokenManager.AccessToken;

    /// <summary>Limit and remaining quota reported by the last response.</summary>
    public RateLimitInfo RateLimit => _restClient.RateLimit;

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
        Trading = new TradingService(auth.RestClient);
        Account = new AccountService(auth.RestClient);
        Portfolio = new PortfolioService(auth.RestClient, auth.Config);
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

    /// <summary>
    /// Every (re)connect asks the auth client for a token, so a long-lived stream survives token expiry;
    /// after a 401/403 handshake it forces one refresh before giving up.
    /// </summary>
    public StreamClient(AuthClient auth)
    {
        var tokens = auth.TokenManager;
        _wsClient = new WebSocketClient(
            auth.Config,
            tokenProvider: async (force, ct) =>
            {
                if (force && tokens.HasRefreshToken) await auth.RefreshAsync(ct).ConfigureAwait(false);
                return await auth.EnsureAuthenticatedAsync(ct: ct).ConfigureAwait(false);
            },
            expiryProvider: () => tokens.Token?.EffectiveExpiresAt ?? 0);
        var token = auth.AccessToken;
        if (!string.IsNullOrEmpty(token))
            _wsClient.SetToken(token);
        Streaming = new StreamingService(_wsClient);
    }

    public Task ConnectAsync(CancellationToken ct = default) => _wsClient.ConnectAsync(ct);

    public async Task DisconnectAsync()
    {
        Streaming.StopPingLoop();
        await _wsClient.DisconnectAsync().ConfigureAwait(false);
    }

    public void Disconnect()
    {
        Streaming.StopPingLoop();
        _wsClient.Disconnect();
    }

    public bool IsConnected => _wsClient.IsConnected;

    /// <summary>The error that ended the connection for good; null while healthy.</summary>
    public Exception? LastError => _wsClient.LastError;

    /// <summary>How many times the socket was re-opened after a drop.</summary>
    public int ReconnectCount => _wsClient.ReconnectCount;

    /// <summary>connected / reconnected / disconnected / failed.</summary>
    public Action<ConnectionEvent>? OnConnection
    {
        get => _wsClient.OnState;
        set => _wsClient.OnState = value;
    }

    /// <summary>Blocks until the client stops; <paramref name="timeoutMs"/> (milliseconds) bounds the wait, null waits forever.</summary>
    public Task WaitAsync(int? timeoutMs = null) => _wsClient.WaitAsync(timeoutMs);

    public void Dispose()
    {
        Disconnect();
    }
}

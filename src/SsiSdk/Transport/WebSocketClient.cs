using System.Text.RegularExpressions;
using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Transport;

/// <summary>
/// Supervised streaming socket: opens with a fresh token each time, re-sends the subscriptions after a
/// reconnect, backs off with jitter, re-opens before the token expires, and stops on errors retrying cannot fix.
/// </summary>
internal sealed partial class WebSocketClient : IDisposable
{
    public delegate void MessageHandler(JsonElement message);

    private static readonly Logger Log = new("ssi_sdk.transport.websocket");

    private readonly Config _config;
    private readonly Func<bool, CancellationToken, Task<string?>>? _tokenProvider;
    private readonly Func<double>? _expiryProvider;
    private readonly bool _autoReconnect;
    private readonly WsConnectFn _connectFn;
    private readonly Func<TimeSpan, CancellationToken, Task> _sleep;
    private readonly Func<double> _random;
    private readonly Func<double> _clock;

    private readonly object _lock = new();
    private readonly object _handlerLock = new();
    private readonly Dictionary<string, List<MessageHandler>> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly SubscriptionBook _book = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private IWsConnection? _connection;
    private Task? _supervisor;
    private CancellationTokenSource? _supervisorCts;
    private string? _token;
    private volatile bool _closing;
    private JsonElement? _rejection;
    private TaskCompletionSource _rejected = NewSignal();
    private Exception? _lastError;
    private int _reconnectCount;

    /// <summary>Called on every connection state change; errors it throws are logged, never propagated.</summary>
    public Action<ConnectionEvent>? OnState { get; set; }

    public WebSocketClient(
        Config config,
        Func<bool, CancellationToken, Task<string?>>? tokenProvider = null,
        Func<double>? expiryProvider = null,
        bool? autoReconnect = null,
        WsConnectFn? connectFn = null,
        Func<TimeSpan, CancellationToken, Task>? sleepFn = null,
        Func<double>? randomFn = null,
        Func<double>? clock = null)
    {
        _config = config;
        _tokenProvider = tokenProvider;
        _expiryProvider = expiryProvider;
        _autoReconnect = autoReconnect ?? config.AutoReconnect;
        _connectFn = connectFn ?? ClientWebSocketConnection.ConnectAsync;
        _sleep = sleepFn ?? ((delay, ct) => Task.Delay(delay, ct));
        _random = randomFn ?? Random.Shared.NextDouble;
        _clock = clock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0);
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Static token, used when no token provider was given.</summary>
    public void SetToken(string token)
    {
        lock (_lock) _token = token;
    }

    /// <summary>The error that ended the supervisor (a connection that could not be recovered).</summary>
    public Exception? LastError => _lastError;

    /// <summary>How many times the socket was re-opened after the first connect.</summary>
    public int ReconnectCount => Volatile.Read(ref _reconnectCount);

    public bool IsConnected
    {
        get
        {
            lock (_lock) return _connection is not null && _supervisor is { IsCompleted: false };
        }
    }

    private void Emit(string state, Exception? error = null)
    {
        var handler = OnState;
        if (handler is null) return;
        try
        {
            handler(new ConnectionEvent(state, ReconnectCount, error));
        }
        catch (Exception ex)
        {
            Log.Error($"Error in the OnConnection handler: {ex.GetType().Name}");
        }
    }

    /// <summary>Open the socket and keep it open. Returns once the first connection is up.</summary>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        TaskCompletionSource ready;
        lock (_lock)
        {
            if (_supervisor is { IsCompleted: false }) return;
            RequireWss(_config.StreamingUrl);
            _closing = false;
            _lastError = null;
            ready = NewSignal();
            _supervisorCts = new CancellationTokenSource();
            var token = _supervisorCts.Token;
            _supervisor = Task.Run(() => SuperviseAsync(ready, token), CancellationToken.None);
        }

        try
        {
            await ready.Task.WaitAsync(ct);
        }
        catch
        {
            await DisconnectAsync();
            throw;
        }
    }

    /// <summary>Stop the supervisor, close the socket and forget the subscriptions.</summary>
    public async Task DisconnectAsync()
    {
        _closing = true;
        Task? supervisor;
        CancellationTokenSource? cts;
        lock (_lock)
        {
            supervisor = _supervisor;
            cts = _supervisorCts;
            _supervisor = null;
            _supervisorCts = null;
        }

        if (cts is not null)
        {
            try { cts.Cancel(); } catch (ObjectDisposedException) { }
        }
        if (supervisor is not null)
        {
            try { await supervisor; } catch { /* the supervisor reports its own failure */ }
        }
        cts?.Dispose();
        await CloseConnectionAsync();
        _book.Clear();
        Log.Info("WebSocket disconnected");
    }

    public void Disconnect() => Task.Run(DisconnectAsync).GetAwaiter().GetResult();

    /// <summary>Register the one handler for a route (a channel name, or an "@" route); null removes it.</summary>
    public void On(string channel, MessageHandler? handler)
    {
        lock (_handlerLock)
        {
            if (handler is null) _handlers.Remove(channel);
            else _handlers[channel] = [handler];
        }
    }

    public void Off(string channel, MessageHandler? handler = null)
    {
        lock (_handlerLock)
        {
            if (!_handlers.TryGetValue(channel, out var list)) return;
            if (handler is null) _handlers.Remove(channel);
            else list.RemoveAll(h => h == handler);
        }
    }

    public async Task SendAsync(WsRequest request, CancellationToken ct = default)
    {
        var connection = _connection ?? throw new WebSocketException("Not connected. Call ConnectAsync() first.");
        try
        {
            foreach (var part in request.Split())
                await SendFrameAsync(connection, part.ToJson(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new WebSocketException($"Failed to send message: {ex.GetType().Name}", ex);
        }
        _book.Record(request);
    }

    private async Task SendFrameAsync(IWsConnection connection, string text, CancellationToken ct)
    {
        await _sendLock.WaitAsync(ct);
        try
        {
            await connection.SendAsync(text, ct);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task WaitAsync(int? timeoutMs = null)
    {
        Task? task;
        lock (_lock) task = _supervisor;
        if (task is null) return;
        try
        {
            if (timeoutMs is null) await task;
            else await task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs.Value));
        }
        catch
        {
            // timeout or supervisor failure: LastError holds the failure
        }
    }

    // ── supervisor ───────────────────────────────────────────

    private static void RequireWss(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase))
            throw new WebSocketException($"streaming_url must use wss://, got '{Redact.SafeUrl(url)}'");
    }

    private static AuthenticationException AuthError(int status) =>
        new($"WebSocket authentication failed after refreshing the token ({status})", status.ToString(), status);

    [GeneratedRegex(@"status code '(\d{3})'|HTTP (\d{3})")]
    private static partial Regex StatusRegex();

    internal static int? HandshakeStatus(Exception error)
    {
        if (error is HandshakeException h) return h.Status;
        var m = StatusRegex().Match(error.Message);
        if (!m.Success) return null;
        return int.Parse(m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value);
    }

    private async Task<string?> CurrentTokenAsync(bool forceRefresh, CancellationToken ct)
    {
        if (_tokenProvider is null)
        {
            lock (_lock) return _token;
        }
        return await _tokenProvider(forceRefresh, ct);
    }

    private async Task<IWsConnection> OpenAsync(bool forceRefresh, CancellationToken ct)
    {
        var token = await CurrentTokenAsync(forceRefresh, ct);
        var headers = new Dictionary<string, string>
        {
            [Constants.HeaderContentType] = Constants.ContentTypeJson,
            [Constants.HeaderAccept] = Constants.ContentTypeJson,
        };
        if (!string.IsNullOrEmpty(token)) headers[Constants.HeaderAuthorization] = Constants.AuthSchemeBearer + token;

        Log.Debug($"Connecting to WebSocket {Redact.SafeUrl(_config.StreamingUrl)}");
        var connection = await _connectFn(new Uri(_config.StreamingUrl), headers, _config, ct);
        lock (_lock)
        {
            _connection = connection;
            _rejection = null;
            _rejected = NewSignal();
        }
        Log.Info($"WebSocket connected to {Redact.SafeUrl(_config.StreamingUrl)}");
        return connection;
    }

    private async Task CloseConnectionAsync()
    {
        IWsConnection? connection;
        lock (_lock)
        {
            connection = _connection;
            _connection = null;
        }
        if (connection is null) return;
        try { await connection.CloseAsync(); }
        catch (Exception ex) { Log.Debug($"Error while closing the WebSocket: {ex.GetType().Name}"); }
        await connection.DisposeAsync();
    }

    private async Task ResubscribeAsync(IWsConnection connection, CancellationToken ct)
    {
        foreach (var request in _book.Requests())
        {
            foreach (var part in request.Split())
                await SendFrameAsync(connection, part.ToJson(), ct);
        }
    }

    private async Task<string> ServeAsync(IWsConnection connection, CancellationToken ct)
    {
        using var local = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var listen = ListenAsync(connection, local.Token);
        Task rejected;
        lock (_lock) rejected = _rejected.Task;
        var waiting = new List<Task> { listen, rejected };

        var delay = Reconnect.SecondsUntilRefresh(_expiryProvider?.Invoke() ?? 0, _clock());
        Task? refresh = null;
        if (delay is not null)
        {
            refresh = _sleep(TimeSpan.FromSeconds(delay.Value), local.Token);
            waiting.Add(refresh);
        }

        try
        {
            await Task.WhenAny(waiting);
            string reason;
            if (listen.IsCompleted) reason = "closed";
            else if (rejected.IsCompleted) reason = "rejected";
            else reason = "refresh";
            bool hasRejection;
            lock (_lock) hasRejection = _rejection is not null;
            // A refusal frame is followed by a close; report the cause, not the symptom.
            return reason == "closed" && hasRejection ? "rejected" : reason;
        }
        finally
        {
            local.Cancel();
            // `rejected` is a signal only a server refusal completes; awaiting it here would hang every normal exit.
            foreach (var task in waiting)
            {
                if (ReferenceEquals(task, rejected)) continue;
                try { await task; } catch { /* cancelled */ }
            }
        }
    }

    private void Fail(Exception error, TaskCompletionSource? ready)
    {
        _lastError = error;
        Log.Error($"WebSocket stopped: {error.GetType().Name}");
        Emit(ConnectionState.Failed, error);
        ready?.TrySetException(error);
    }

    private double Backoff(int attempt) => Reconnect.BackoffDelay(attempt, _config.RetryDelayMs / 1000.0, jitter: _random());

    private async Task SuperviseAsync(TaskCompletionSource ready, CancellationToken ct)
    {
        TaskCompletionSource? pending = ready;
        var attempt = 0;
        var authRetried = false;
        var forceRefresh = false;
        try
        {
            while (!_closing && !ct.IsCancellationRequested)
            {
                IWsConnection connection;
                try
                {
                    connection = await OpenAsync(forceRefresh, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (AuthenticationException exc)
                {
                    Fail(exc, pending); // retrying cannot fix this: a new OTP is needed
                    return;
                }
                catch (Exception exc)
                {
                    var status = HandshakeStatus(exc);
                    if (status is 401 or 403)
                    {
                        if (authRetried)
                        {
                            Fail(AuthError(status.Value), pending);
                            return;
                        }
                        authRetried = true;
                        forceRefresh = true;
                        continue;
                    }
                    var wait = status == 429 ? Reconnect.RateLimitWait(null) : Backoff(attempt);
                    attempt++;
                    if (pending is not null && attempt >= Math.Max(1, _config.MaxRetries))
                    {
                        Fail(new WebSocketException(
                            $"Failed to connect to {Redact.SafeUrl(_config.StreamingUrl)} after {attempt} attempts: {exc.GetType().Name}"),
                            pending);
                        return;
                    }
                    Log.Warn($"WebSocket connect attempt {attempt} failed ({exc.GetType().Name}); retrying in {wait:F1}s");
                    await _sleep(TimeSpan.FromSeconds(wait), ct);
                    continue;
                }

                var openedAt = _clock();
                forceRefresh = false;
                try
                {
                    await ResubscribeAsync(connection, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Log.Warn($"Could not restore subscriptions: {ex.GetType().Name}");
                }

                if (pending is not null)
                {
                    pending.TrySetResult();
                    pending = null;
                    Emit(ConnectionState.Connected);
                }
                else
                {
                    var count = Interlocked.Increment(ref _reconnectCount);
                    Log.Info($"WebSocket reconnected (#{count})");
                    Emit(ConnectionState.Reconnected);
                }

                var reason = await ServeAsync(connection, ct);
                await CloseConnectionAsync();
                if (_closing || ct.IsCancellationRequested) return;
                Emit(ConnectionState.Disconnected);
                var stable = _clock() - openedAt > Constants.WsStableMs / 1000.0;
                attempt = stable ? 0 : attempt + 1;

                double waitSeconds;
                if (reason == "refresh")
                {
                    forceRefresh = true; // the old token is about to expire: get a new one
                    continue;
                }
                if (reason == "rejected")
                {
                    JsonElement? rejection;
                    lock (_lock) rejection = _rejection;
                    var (kind, retryAfter) = Reconnect.ClassifyRejection(rejection);
                    if (kind == RejectionKind.Auth)
                    {
                        if (authRetried)
                        {
                            Fail(AuthError(401), null);
                            return;
                        }
                        authRetried = true;
                        forceRefresh = true;
                        continue;
                    }
                    waitSeconds = kind == RejectionKind.RateLimited ? Reconnect.RateLimitWait(retryAfter) : Backoff(attempt);
                }
                else
                {
                    authRetried = false;
                    if (!_autoReconnect) return;
                    waitSeconds = Backoff(attempt);
                }
                Log.Info($"WebSocket closed; reconnecting in {waitSeconds:F1}s");
                await _sleep(TimeSpan.FromSeconds(waitSeconds), ct);
            }
        }
        catch (OperationCanceledException)
        {
            // disconnect requested
        }
        finally
        {
            await CloseConnectionAsync();
            pending?.TrySetCanceled();
        }
    }

    // ── receive ──────────────────────────────────────────────

    private async Task ListenAsync(IWsConnection connection, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var raw = await connection.ReceiveAsync(ct);
                if (raw is null)
                {
                    Log.Info("WebSocket connection closed");
                    return;
                }
                var message = Dispatch.Decode(raw);
                if (message is null)
                {
                    Log.Warn($"Received non-JSON message ({raw.Length} chars)");
                    continue;
                }
                var routes = Dispatch.RoutesOf(message.Value);
                if (routes.Contains(Constants.WsRouteError))
                {
                    lock (_lock) _rejection = message;
                    _rejected.TrySetResult();
                }
                DispatchMessage(message.Value, routes);
            }
        }
        catch (OperationCanceledException)
        {
            // serving ended
        }
        catch (Exception ex)
        {
            Log.Error($"WebSocket listen error: {ex.GetType().Name}");
        }
    }

    private void DispatchMessage(JsonElement message, List<string> routes)
    {
        if (Dispatch.AckErrorCategory(message) is { } category)
        {
            Log.Warn($"Server refused a {Converter.ToStr(Converter.GetProp(message, "method"))} request on " +
                     $"{Converter.ToStr(Converter.GetProp(message, "channel"))}: {category}");
        }
        foreach (var route in routes)
        {
            MessageHandler[] handlers;
            lock (_handlerLock)
                handlers = _handlers.TryGetValue(route, out var list) ? list.ToArray() : [];
            foreach (var handler in handlers)
            {
                try
                {
                    handler(message);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in handler for route '{route}': {ex.GetType().Name}");
                }
            }
        }
    }

    public void Dispose() => Disconnect();
}

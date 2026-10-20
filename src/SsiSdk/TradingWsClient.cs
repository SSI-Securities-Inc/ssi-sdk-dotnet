using System.Collections.Concurrent;
using System.Text.Json;
using SsiSdk.Internal;
using SsiSdk.Transport;

namespace SsiSdk;

/// <summary>Knobs of the trading WebSocket client.</summary>
public sealed class TradingWsOptions
{
    /// <summary>Milliseconds a command waits for its first response frame. Null takes <c>Config.TimeoutMs</c> (10000 when unset).</summary>
    public int? RequestTimeoutMs { get; set; }

    /// <summary>"auto" (base64, one retry in hex when the server refuses the signature), "base64" or "hex".</summary>
    public string SignatureEncoding { get; set; } = "auto";

    /// <summary>The server accepts the socket and may refuse it right after; wait this many milliseconds for that frame.</summary>
    public int HandshakeGraceMs { get; set; } = 500;

    /// <summary>At remaining == 0, hold the next command until the quota resets (capped by <see cref="MaxRateLimitWaitMs"/>).</summary>
    public bool RespectRateLimit { get; set; } = true;

    /// <summary>Upper bound, in milliseconds, of the hold applied when the quota is exhausted.</summary>
    public int MaxRateLimitWaitMs { get; set; } = 2000;

    /// <summary>Keep-alive PING_PONG interval in milliseconds; null or zero turns it off.</summary>
    public int? PingIntervalMs { get; set; } = 30_000;

    /// <summary>Null takes <c>Config.UserAgent</c>.</summary>
    public string? UserAgent { get; set; }

    internal TradingWsOptions Clone() => (TradingWsOptions)MemberwiseClone();
}

/// <summary>
/// Client for the trading WebSocket (<c>/ws/v3/trading</c>).
/// It does not reconnect: after a drop, create a new one.
/// </summary>
public sealed class TradingWsClient : IAsyncDisposable
{
    private static readonly Logger Log = new("ssi_sdk.trading_ws");

    private readonly Config _config;
    private readonly Func<CancellationToken, Task<string>> _tokenProvider;
    private readonly string? _privateKey;
    private readonly TradingWsOptions _options;
    private readonly WsConnectFn _connect;
    private readonly Func<double> _clock;
    private readonly Func<TimeSpan, CancellationToken, Task> _sleep;
    private readonly bool _auto;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<Outcome>> _pending = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private string _encoding;
    private IWsConnection? _ws;
    private CancellationTokenSource? _cts;
    private Task? _reader;
    private Task? _pinger;
    private volatile TradingWsError? _rejection;
    private double _blockedUntil;

    private sealed record Outcome(JsonElement? Frame, TradingWsError? Error);

    /// <summary>Handler for frames no command is waiting for: events, acks, later frames of a command.</summary>
    public Action<JsonElement>? OnEvent { get; set; }

    /// <summary>The quota the server reported with the last response.</summary>
    public IReadOnlyDictionary<string, JsonElement> RateLimit { get; private set; } = new Dictionary<string, JsonElement>();

    public TradingWsClient(Config config, Func<CancellationToken, Task<string>> tokenProvider, TradingWsOptions? options = null)
        : this(config, tokenProvider, options, null, null, null) { }

    internal TradingWsClient(
        Config config, Func<CancellationToken, Task<string>> tokenProvider, TradingWsOptions? options,
        WsConnectFn? connect, Func<double>? clock, Func<TimeSpan, CancellationToken, Task>? sleep)
    {
        _config = config;
        _tokenProvider = tokenProvider;
        _privateKey = config.PrivateKey;
        _options = options ?? new TradingWsOptions();
        RequireWss(config.TradingWsUrl);
        if (_options.SignatureEncoding is not ("auto" or "base64" or "hex"))
            throw new ValidationException($"SignatureEncoding must be auto, base64 or hex, got {_options.SignatureEncoding}");
        _auto = _options.SignatureEncoding == "auto";
        _encoding = _auto ? "base64" : _options.SignatureEncoding;
        _connect = connect ?? ClientWebSocketConnection.ConnectAsync;
        _clock = clock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0);
        _sleep = sleep ?? ((delay, ct) => Task.Delay(delay, ct));
    }

    private static void RequireWss(string url)
    {
        if (!url.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
            throw new WebSocketException($"trading_ws_url must use wss://, got '{Redact.SafeUrl(url)}'");
    }

    public bool IsConnected => _ws is not null && _reader is { IsCompleted: false };

    /// <summary>The refusal the server sent after the upgrade (401/403/429/500), if any.</summary>
    public TradingWsError? Rejection => _rejection;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_ws is not null) return;
        var token = await _tokenProvider(ct).ConfigureAwait(false);
        _rejection = null;
        var headers = new Dictionary<string, string>
        {
            [Constants.HeaderAuthorization] = Constants.AuthSchemeBearer + token,
            [Constants.HeaderUserAgent] = string.IsNullOrWhiteSpace(_options.UserAgent) ? Constants.DefaultUserAgent : _options.UserAgent!,
        };
        var ws = await _connect(new Uri(_config.TradingWsUrl), headers, _config, ct).ConfigureAwait(false);
        _ws = ws;
        _cts = new CancellationTokenSource();
        _reader = Task.Run(() => ReadLoopAsync(ws, _cts.Token));

        // The server accepts the socket (101) and refuses afterwards: give that frame time to show.
        const int stepMs = 50;
        var waitedMs = 0;
        while (waitedMs < _options.HandshakeGraceMs && _rejection is null && !_reader.IsCompleted)
        {
            await _sleep(TimeSpan.FromMilliseconds(stepMs), ct).ConfigureAwait(false);
            waitedMs += stepMs;
        }
        if (_rejection is { } error)
        {
            await CloseAsync().ConfigureAwait(false);
            throw error;
        }
        if (_options.PingIntervalMs is { } intervalMs && intervalMs > 0)
            _pinger = Task.Run(() => PingLoopAsync(TimeSpan.FromMilliseconds(intervalMs), _cts.Token));
    }

    private async Task PingLoopAsync(TimeSpan interval, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await _sleep(interval, ct).ConfigureAwait(false);
                try { await PingAsync(ct).ConfigureAwait(false); }
                catch (WebSocketException) { return; }
            }
        }
        catch (OperationCanceledException) { }
    }

    public async Task CloseAsync()
    {
        var cts = _cts;
        _cts = null;
        try { cts?.Cancel(); } catch (ObjectDisposedException) { }
        var ws = _ws;
        _ws = null;
        if (ws is not null)
        {
            try { await ws.CloseAsync().ConfigureAwait(false); }
            catch (Exception ex) { Log.Debug($"Error while closing the trading WebSocket: {ex.GetType().Name}"); }
            await ws.DisposeAsync().ConfigureAwait(false);
        }
        foreach (var task in new[] { _pinger, _reader })
        {
            if (task is null) continue;
            try { await task.WaitAsync(TimeSpan.FromMilliseconds(Constants.WsThreadJoinTimeoutMs)).ConfigureAwait(false); }
            catch (Exception) { /* the reader/pinger end on their own once the socket is gone */ }
        }
        _pinger = null;
        _reader = null;
        FailPending(null);
        cts?.Dispose();
    }

    public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);

    private void FailPending(TradingWsError? error)
    {
        foreach (var key in _pending.Keys)
            if (_pending.TryRemove(key, out var waiting))
                waiting.TrySetResult(new Outcome(null, error));
    }

    private string NewId()
    {
        while (true)
        {
            var id = TradingWsProtocol.NewRequestId();
            if (!_pending.ContainsKey(id)) return id;
        }
    }

    /// <summary>Sends one command and returns its first response frame (the ack). A command is never retried.</summary>
    public async Task<TradingWsResponse> RequestAsync(string method, object? parameters, CancellationToken ct = default)
    {
        try
        {
            return await RequestOnceAsync(method, parameters, ct).ConfigureAwait(false);
        }
        catch (TradingWsError error) when (OtherEncoding(method, error) is not null)
        {
            // The server refused the signature, so the command was NOT executed: try the other form.
            var other = OtherEncoding(method, error)!;
            var first = _encoding;
            _encoding = other;
            try
            {
                return await RequestOnceAsync(method, parameters, ct).ConfigureAwait(false);
            }
            catch (TradingWsError second)
            {
                _encoding = first;
                throw TradingWsProtocol.BothRefused(second);
            }
        }
    }

    private string? OtherEncoding(string method, TradingWsError error)
    {
        if (!_auto || !TradingWsProtocol.IsMutating(method)) return null;
        if (!TradingWsProtocol.SignatureCodes.Contains(error.ErrorCode)) return null;
        return _encoding == "base64" ? "hex" : "base64";
    }

    private async Task<TradingWsResponse> RequestOnceAsync(string method, object? parameters, CancellationToken ct)
    {
        if (_rejection is { } rejected) throw rejected;
        var ws = _ws ?? throw new WebSocketException("not connected");

        if (_options.RespectRateLimit && _blockedUntil > _clock())
            await _sleep(TimeSpan.FromSeconds(_blockedUntil - _clock()), ct).ConfigureAwait(false);

        var id = NewId();
        var raw = TradingWsProtocol.BuildCommand(
            method, parameters, _privateKey, id, (long)(_clock() * 1000), _encoding);
        var waiting = new TaskCompletionSource<Outcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = waiting;
        try
        {
            await SendTextAsync(ws, raw, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _pending.TryRemove(id, out _);
            throw new WebSocketException($"failed to send {method}: {ex.GetType().Name}");
        }

        try
        {
            var outcome = await waiting.Task.WaitAsync(TimeSpan.FromMilliseconds(_options.RequestTimeoutMs ?? 10_000), ct).ConfigureAwait(false);
            if (outcome.Error is not null) throw outcome.Error;
            if (outcome.Frame is not { } frame)
                throw new WebSocketException("connection closed before the response arrived");
            var hint = _auto ? null : _encoding;
            var response = TradingWsResponse.FromFrame(TradingWsProtocol.Unwrap(frame, hint));
            NoteRateLimit(response.RateLimit);
            return response;
        }
        catch (TimeoutException)
        {
            throw new WebSocketException("timed out waiting for a response (not retried)");
        }
        finally
        {
            _pending.TryRemove(id, out _);
        }
    }

    private void NoteRateLimit(IReadOnlyDictionary<string, JsonElement> rateLimit)
    {
        if (rateLimit.Count == 0) return;
        RateLimit = rateLimit;
        if (rateLimit.TryGetValue("remaining", out var remaining)
            && remaining.ValueKind == JsonValueKind.Number && remaining.TryGetDouble(out var left) && left == 0)
        {
            var resetMs = rateLimit.TryGetValue("resetMs", out var r) && r.ValueKind == JsonValueKind.Number && r.TryGetDouble(out var ms) ? ms : 0;
            var wait = Math.Min(resetMs / 1000.0, _options.MaxRateLimitWaitMs / 1000.0);
            _blockedUntil = _clock() + wait;
        }
    }

    /// <summary>Sends a subscription frame (no id, so the server treats it as a subscription).</summary>
    public Task SubscribeAsync(string channel, IEnumerable<string> topics, CancellationToken ct = default) =>
        SendFrameAsync("SUBSCRIBE", channel, topics, ct);

    public Task UnsubscribeAsync(string channel, IEnumerable<string> topics, CancellationToken ct = default) =>
        SendFrameAsync("UNSUBSCRIBE", channel, topics, ct);

    /// <summary>Sends PING_PONG; the ack arrives on <see cref="OnEvent"/>.</summary>
    public async Task PingAsync(CancellationToken ct = default)
    {
        var ws = _ws ?? throw new WebSocketException("not connected");
        await SendFrameTextAsync(ws, "{\"method\":\"PING_PONG\"}", ct).ConfigureAwait(false);
    }

    private async Task SendFrameAsync(string method, string channel, IEnumerable<string> topics, CancellationToken ct)
    {
        var ws = _ws ?? throw new WebSocketException("not connected");
        var text = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["method"] = method,
            ["channel"] = channel,
            ["topics"] = topics.ToList(),
        });
        await SendFrameTextAsync(ws, text, ct).ConfigureAwait(false);
    }

    private async Task SendFrameTextAsync(IWsConnection ws, string text, CancellationToken ct)
    {
        try { await SendTextAsync(ws, text, ct).ConfigureAwait(false); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new WebSocketException($"failed to send: {ex.GetType().Name}");
        }
    }

    private async Task SendTextAsync(IWsConnection ws, string text, CancellationToken ct)
    {
        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try { await ws.SendAsync(text, ct).ConfigureAwait(false); }
        finally { _sendLock.Release(); }
    }

    private async Task ReadLoopAsync(IWsConnection ws, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var text = await ws.ReceiveAsync(ct).ConfigureAwait(false);
                if (text is null) break;
                if (TradingWsProtocol.Decode(text) is not { } frame)
                {
                    Log.Warn($"Dropped a non-JSON trading frame ({text.Length} chars)");
                    continue;
                }
                if (TradingWsProtocol.IsRejection(frame))
                {
                    _rejection = TradingWsProtocol.RejectionError(frame);
                    FailPending(_rejection);
                    continue;
                }
                var id = frame.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String ? idEl.GetString() : null;
                if (id is not null && _pending.TryRemove(id, out var waiting))
                {
                    waiting.TrySetResult(new Outcome(frame, null));
                }
                else if (OnEvent is { } handler)
                {
                    try { handler(frame); }
                    catch (Exception ex) { Log.Error($"Error in the OnEvent handler: {ex.GetType().Name}"); }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Log.Debug($"Trading WebSocket reader ended: {ex.GetType().Name}");
        }
        finally
        {
            FailPending(null);
        }
    }
}

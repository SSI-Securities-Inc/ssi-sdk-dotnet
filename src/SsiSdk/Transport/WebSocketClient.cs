using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Transport;

internal sealed class WebSocketClient : IDisposable
{
    public delegate void MessageHandler(JsonElement message);

    private static readonly Logger Log = new("ssi_sdk.transport.websocket");

    private readonly Config _config;
    private ClientWebSocket? _ws;
    private readonly Dictionary<string, MessageHandler> _handlers = new();
    private string _token = string.Empty;
    private volatile bool _running;
    private readonly object _lock = new();
    private readonly object _handlerLock = new();
    private TaskCompletionSource _done = new();

    public WebSocketClient(Config config)
    {
        _config = config;
    }

    public void SetToken(string token)
    {
        lock (_lock) _token = token;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_running) return;
        }

        var maxRetries = _config.MaxRetries;
        var delay = _config.RetryDelay;
        Exception? lastErr = null;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var ws = new ClientWebSocket();
                ws.Options.SetRequestHeader(Constants.HeaderContentType, Constants.ContentTypeJson);
                ws.Options.SetRequestHeader(Constants.HeaderAccept, Constants.ContentTypeJson);

                lock (_lock)
                {
                    if (!string.IsNullOrEmpty(_token))
                        ws.Options.SetRequestHeader(Constants.HeaderAuthorization, Constants.AuthSchemeBearer + _token);
                }

                if (!string.IsNullOrEmpty(_config.Proxy))
                    ws.Options.Proxy = new WebProxy(_config.Proxy);

                var uri = new Uri(_config.StreamingUrl);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(_config.TimeoutSeconds));
                await ws.ConnectAsync(uri, cts.Token);

                lock (_lock)
                {
                    _ws = ws;
                    _running = true;
                    _done = new TaskCompletionSource();
                }

                _ = Task.Run(() => ListenAsync(), CancellationToken.None);
                Log.Info($"WebSocket connected to {_config.StreamingUrl}");
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                lastErr = ex;
                if (attempt < maxRetries)
                {
                    var wait = delay * Math.Pow(2, attempt);
                    Log.Error($"WebSocket connect attempt {attempt + 1}/{maxRetries + 1} failed: {ex.Message}. Retrying in {wait:F1}s");
                    await Task.Delay(TimeSpan.FromSeconds(wait), ct);
                }
            }
        }

        throw new WebSocketException(
            $"Failed to connect to {_config.StreamingUrl} after {maxRetries + 1} attempts: {lastErr?.Message}");
    }

    public void Disconnect()
    {
        ClientWebSocket? ws;
        lock (_lock)
        {
            _running = false;
            ws = _ws;
            _ws = null;
        }

        if (ws is not null)
        {
            try
            {
                ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "disconnect", CancellationToken.None)
                    .GetAwaiter().GetResult();
            }
            catch { /* best effort */ }
            ws.Dispose();
        }

        _done.Task.Wait(Constants.WsThreadJoinTimeoutMs);
        Log.Info("WebSocket disconnected");
    }

    public void On(string channel, MessageHandler? handler)
    {
        lock (_handlerLock)
        {
            if (handler is null)
                _handlers.Remove(channel);
            else
                _handlers[channel] = handler;
        }
    }

    public async Task SendAsync(object message, CancellationToken ct = default)
    {
        ClientWebSocket? ws;
        lock (_lock) ws = _ws;

        if (ws is null || !_running)
            throw new WebSocketException("Not connected. Call ConnectAsync() first.");

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }

    public bool IsConnected
    {
        get
        {
            lock (_lock) return _running && _ws?.State == System.Net.WebSockets.WebSocketState.Open;
        }
    }

    public Task WaitAsync(TimeSpan? timeout = null)
    {
        if (timeout is null) return _done.Task;
        return Task.WhenAny(_done.Task, Task.Delay(timeout.Value));
    }

    private async Task ListenAsync()
    {
        var buffer = new byte[8192];
        var messageBuffer = new MemoryStream();

        try
        {
            while (true)
            {
                ClientWebSocket? ws;
                lock (_lock)
                {
                    ws = _ws;
                    if (!_running || ws is null) return;
                }

                WebSocketReceiveResult result;
                try
                {
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                }
                catch
                {
                    lock (_lock) { if (!_running) return; }
                    Log.Error("WebSocket read error");
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Close) return;

                messageBuffer.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage) continue;

                var raw = messageBuffer.ToArray();
                messageBuffer.SetLength(0);

                try
                {
                    using var doc = JsonDocument.Parse(raw);
                    var el = doc.RootElement.Clone();

                    var channel = Converter.ToStr(Converter.GetProp(el, "channel"));
                    MessageHandler? handler;
                    lock (_handlerLock)
                    {
                        _handlers.TryGetValue(channel, out handler);
                    }
                    if (handler is not null)
                    {
                        try { handler(el); }
                        catch (Exception ex) { Log.Error($"Error in handler for channel '{channel}': {ex.Message}"); }
                    }
                }
                catch
                {
                    Log.Debug($"Received non-JSON message: {Encoding.UTF8.GetString(raw)}");
                }
            }
        }
        finally
        {
            lock (_lock)
            {
                _running = false;
                _ws = null;
            }
            _done.TrySetResult();
            messageBuffer.Dispose();
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}

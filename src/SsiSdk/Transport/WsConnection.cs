using System.Net;
using System.Net.WebSockets;
using System.Text;
using SsiSdk.Internal;

namespace SsiSdk.Transport;

/// <summary>One open text-frame socket. Kept tiny so the supervisor can be tested without a network.</summary>
internal interface IWsConnection : IAsyncDisposable
{
    Task SendAsync(string text, CancellationToken ct);

    /// <summary>The next text frame, or null when the peer closed the socket.</summary>
    Task<string?> ReceiveAsync(CancellationToken ct);

    Task CloseAsync();
}

/// <summary>The server answered the WebSocket handshake with an HTTP error status.</summary>
internal sealed class HandshakeException : Exception
{
    public int Status { get; }

    public HandshakeException(int status, Exception inner)
        : base($"WebSocket handshake failed with HTTP {status}", inner) => Status = status;
}

internal delegate Task<IWsConnection> WsConnectFn(
    Uri uri, IReadOnlyDictionary<string, string> headers, Config config, CancellationToken ct);

internal sealed class ClientWebSocketConnection : IWsConnection
{
    private readonly ClientWebSocket _ws;
    private readonly byte[] _buffer = new byte[8192];

    private ClientWebSocketConnection(ClientWebSocket ws) => _ws = ws;

    public static async Task<IWsConnection> ConnectAsync(
        Uri uri, IReadOnlyDictionary<string, string> headers, Config config, CancellationToken ct)
    {
        var ws = new ClientWebSocket();
        try
        {
            foreach (var (name, value) in headers) ws.Options.SetRequestHeader(name, value);
            ws.Options.CollectHttpResponseDetails = true;
            if (!string.IsNullOrEmpty(config.Proxy)) ws.Options.Proxy = new WebProxy(config.Proxy);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMilliseconds(config.TimeoutMs));
            await ws.ConnectAsync(uri, cts.Token);
            return new ClientWebSocketConnection(ws);
        }
        catch (System.Net.WebSockets.WebSocketException ex) when ((int)ws.HttpStatusCode >= 400)
        {
            var status = (int)ws.HttpStatusCode;
            ws.Dispose();
            throw new HandshakeException(status, ex);
        }
        catch
        {
            ws.Dispose();
            throw;
        }
    }

    public Task SendAsync(string text, CancellationToken ct) =>
        _ws.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, ct);

    public async Task<string?> ReceiveAsync(CancellationToken ct)
    {
        using var message = new MemoryStream();
        while (true)
        {
            var result = await _ws.ReceiveAsync(_buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close) return null;
            message.Write(_buffer, 0, result.Count);
            if (result.EndOfMessage) return Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
        }
    }

    public async Task CloseAsync()
    {
        if (_ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(Constants.WsThreadJoinTimeoutMs));
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "disconnect", cts.Token);
        }
    }

    public ValueTask DisposeAsync()
    {
        _ws.Dispose();
        return ValueTask.CompletedTask;
    }
}

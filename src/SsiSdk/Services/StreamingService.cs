using System.Text.Json;
using SsiSdk.Internal;
using SsiSdk.Models;
using SsiSdk.Transport;

namespace SsiSdk.Services;

public sealed class StreamingService
{
    private static readonly Logger Log = new("ssi_sdk.stream");
    private readonly WebSocketClient _ws;

    private Action<object>? _onData;
    private Action<object>? _onTrading;
    private Action<HeartbeatMessage>? _onHeartbeat;
    private CancellationTokenSource? _pingCts;

    internal StreamingService(WebSocketClient ws) => _ws = ws;

    public void SetOnData(Action<object>? callback)
    {
        _onData = callback;
        if (callback is null)
        {
            _ws.On(StreamingChannel.Data, null);
            return;
        }
        _ws.On(StreamingChannel.Data, msg =>
        {
            var topic = Converter.ToStr(Converter.GetProp(msg, "topic"));
            var data = Converter.GetProp(msg, "data");
            if (data is null || data.Value.ValueKind != JsonValueKind.Object)
            {
                callback(msg);
                return;
            }
            callback(StreamingMessageParser.ParseDataMessage(topic, data.Value));
        });
    }

    public void SetOnTrading(Action<object>? callback)
    {
        _onTrading = callback;
        if (callback is null)
        {
            _ws.On(StreamingChannel.Trading, null);
            return;
        }
        _ws.On(StreamingChannel.Trading, msg =>
        {
            var topic = Converter.ToStr(Converter.GetProp(msg, "topic"));
            var data = Converter.GetProp(msg, "data");
            if (data is null || data.Value.ValueKind != JsonValueKind.Object)
            {
                callback(msg);
                return;
            }
            callback(StreamingMessageParser.ParseTradingMessage(topic, data.Value));
        });
    }

    public void SetOnHeartbeat(Action<HeartbeatMessage>? callback)
    {
        _onHeartbeat = callback;
        if (callback is null)
        {
            _ws.On(StreamingChannel.Heartbeat, null);
            return;
        }
        _ws.On(StreamingChannel.Heartbeat, msg => callback(HeartbeatMessage.FromJson(msg)));
    }

    private Task SubscribeAsync(string method, string channel, string[] topics, CancellationToken ct) =>
        _ws.SendAsync(new { method, channel, topics }, ct);

    public async Task PingAsync(double? intervalSeconds = null, CancellationToken ct = default)
    {
        if (intervalSeconds is null)
        {
            await SubscribeAsync(StreamingMethod.PingPong, StreamingChannel.Heartbeat, [], ct);
            return;
        }

        StopPingLoop();
        _pingCts = new CancellationTokenSource();
        var localCts = _pingCts;

        _ = Task.Run(async () =>
        {
            try
            {
                while (!localCts.Token.IsCancellationRequested && _ws.IsConnected)
                {
                    Log.Debug("Sending ping to WebSocket server");
                    await SubscribeAsync(StreamingMethod.PingPong, StreamingChannel.Heartbeat, [], localCts.Token);
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds.Value), localCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Ping loop cancelled");
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ping loop: {ex.Message}");
            }
        }, CancellationToken.None);
    }

    public void StopPingLoop()
    {
        _pingCts?.Cancel();
        _pingCts = null;
    }

    public Task SubscribeSymbolTradeAsync(string[] symbols, CancellationToken ct = default) =>
        SubscribeAsync(StreamingMethod.Subscribe, StreamingChannel.Data, symbols.Select(s => $"trade.{s}").ToArray(), ct);

    public Task SubscribeSymbolQuoteAsync(string[] symbols, CancellationToken ct = default) =>
        SubscribeAsync(StreamingMethod.Subscribe, StreamingChannel.Data, symbols.Select(s => $"quote.{s}").ToArray(), ct);

    public Task SubscribeSymbolRoomAsync(string[] symbols, CancellationToken ct = default) =>
        SubscribeAsync(StreamingMethod.Subscribe, StreamingChannel.Data, symbols.Select(s => $"room.{s}").ToArray(), ct);

    public Task SubscribeSymbolPutThroughAsync(string[] symbols, CancellationToken ct = default) =>
        SubscribeAsync(StreamingMethod.Subscribe, StreamingChannel.Data, symbols.Select(s => $"put.{s}").ToArray(), ct);

    public Task SubscribeSymbolOddLotAsync(string[] symbols, CancellationToken ct = default) =>
        SubscribeAsync(StreamingMethod.Subscribe, StreamingChannel.Data, symbols.Select(s => $"oddlot.{s}").ToArray(), ct);

    public async Task SubscribeSymbolAsync(string[] symbols, CancellationToken ct = default)
    {
        await SubscribeSymbolTradeAsync(symbols, ct);
        await SubscribeSymbolQuoteAsync(symbols, ct);
        await SubscribeSymbolRoomAsync(symbols, ct);
    }

    public async Task SubscribeBoardAsync(string[] boards, CancellationToken ct = default)
    {
        await SubscribeSymbolTradeAsync(boards, ct);
        await SubscribeSymbolQuoteAsync(boards, ct);
        await SubscribeSymbolRoomAsync(boards, ct);
    }

    public async Task SubscribeIndexAsync(string[] indices, CancellationToken ct = default)
    {
        await SubscribeSymbolTradeAsync(indices, ct);
        await SubscribeSymbolQuoteAsync(indices, ct);
        await SubscribeSymbolRoomAsync(indices, ct);
    }

    public Task UnsubscribeSymbolTradeAsync(string[] symbols, CancellationToken ct = default) =>
        SubscribeAsync(StreamingMethod.Unsubscribe, StreamingChannel.Data, symbols.Select(s => $"trade.{s}").ToArray(), ct);

    public Task UnsubscribeSymbolQuoteAsync(string[] symbols, CancellationToken ct = default) =>
        SubscribeAsync(StreamingMethod.Unsubscribe, StreamingChannel.Data, symbols.Select(s => $"quote.{s}").ToArray(), ct);

    public Task UnsubscribeSymbolRoomAsync(string[] symbols, CancellationToken ct = default) =>
        SubscribeAsync(StreamingMethod.Unsubscribe, StreamingChannel.Data, symbols.Select(s => $"room.{s}").ToArray(), ct);

    public Task UnsubscribeSymbolPutThroughAsync(string[] symbols, CancellationToken ct = default) =>
        SubscribeAsync(StreamingMethod.Unsubscribe, StreamingChannel.Data, symbols.Select(s => $"put.{s}").ToArray(), ct);

    public Task UnsubscribeSymbolOddLotAsync(string[] symbols, CancellationToken ct = default) =>
        SubscribeAsync(StreamingMethod.Unsubscribe, StreamingChannel.Data, symbols.Select(s => $"oddlot.{s}").ToArray(), ct);

    public Task SubscribeSymbolOhlcvAsync(string[] symbols, string interval, CancellationToken ct = default) =>
        SubscribeAsync(StreamingMethod.Subscribe, StreamingChannel.Data, symbols.Select(s => $"trade.{s}@{interval}").ToArray(), ct);

    public Task UnsubscribeSymbolOhlcvAsync(string[] symbols, string interval, CancellationToken ct = default) =>
        SubscribeAsync(StreamingMethod.Unsubscribe, StreamingChannel.Data, symbols.Select(s => $"trade.{s}@{interval}").ToArray(), ct);

    public async Task UnsubscribeSymbolAsync(string[] symbols, CancellationToken ct = default)
    {
        await UnsubscribeSymbolTradeAsync(symbols, ct);
        await UnsubscribeSymbolQuoteAsync(symbols, ct);
        await UnsubscribeSymbolRoomAsync(symbols, ct);
    }

    public async Task UnsubscribeBoardAsync(string[] boards, CancellationToken ct = default)
    {
        await UnsubscribeSymbolTradeAsync(boards, ct);
        await UnsubscribeSymbolQuoteAsync(boards, ct);
        await UnsubscribeSymbolRoomAsync(boards, ct);
    }

    public async Task UnsubscribeIndexAsync(string[] indices, CancellationToken ct = default)
    {
        await UnsubscribeSymbolTradeAsync(indices, ct);
        await UnsubscribeSymbolQuoteAsync(indices, ct);
        await UnsubscribeSymbolRoomAsync(indices, ct);
    }

    public Task SubscribeOrderStatusAsync(string accountNo = "*", CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(accountNo)) accountNo = "*";
        return SubscribeAsync(StreamingMethod.Subscribe, StreamingChannel.Trading, [$"order.{accountNo}"], ct);
    }

    public Task SubscribePortfolioAsync(string accountNo = "*", CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(accountNo)) accountNo = "*";
        return SubscribeAsync(StreamingMethod.Subscribe, StreamingChannel.Trading, [$"portfolio.{accountNo}"], ct);
    }

    public Task WaitAsync(TimeSpan? timeout = null) => _ws.WaitAsync(timeout);
}

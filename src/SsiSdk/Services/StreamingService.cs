using System.Text.Json;
using SsiSdk.Internal;
using SsiSdk.Models;
using SsiSdk.Transport;

namespace SsiSdk.Services;

/// <summary>
/// Real-time streaming over one WebSocket. Callbacks run on the receive loop, so keep them short;
/// an exception thrown by one is logged and never stops the other callbacks or the connection.
/// </summary>
public sealed class StreamingService
{
    private static readonly Logger Log = new("ssi_sdk.stream");
    private readonly WebSocketClient _ws;
    private readonly object _lock = new();

    private Action<object>? _onData;
    private Action<object>? _onTrading;
    private Action<HeartbeatMessage>? _onHeartbeat;
    private Action<MarketFlagMessage>? _onMarketFlag;
    private Action<IndexSummaryMessage>? _onIndexSummary;
    private Action<OrderStatusMessage>? _onOrder;
    private Action<OrderMatchMessage>? _onOrderMatch;
    private Action<PortfolioMessage>? _onPortfolio;
    private CancellationTokenSource? _pingCts;

    internal StreamingService(WebSocketClient ws) => _ws = ws;

    // ── callbacks ────────────────────────────────────────────

    /// <summary>Receives every DATA message, already parsed into its model (raw JsonElement when unknown).</summary>
    public Action<object>? OnData
    {
        get => _onData;
        set { _onData = value; RefreshHandlers(); }
    }

    /// <summary>Receives every TRADING message, already parsed into its model (raw JsonElement when unknown).</summary>
    public Action<object>? OnTrading
    {
        get => _onTrading;
        set { _onTrading = value; RefreshHandlers(); }
    }

    public Action<HeartbeatMessage>? OnHeartbeat
    {
        get => _onHeartbeat;
        set
        {
            _onHeartbeat = value;
            _ws.On(StreamingChannel.Heartbeat, value is null ? null : msg => Deliver(value, HeartbeatMessage.FromJson(msg)));
        }
    }

    /// <summary>Session flags (ATO/LO/ATC...). Subscribes nothing by itself; it only picks where these messages go.</summary>
    public Action<MarketFlagMessage>? OnMarketFlag
    {
        get => _onMarketFlag;
        set { _onMarketFlag = value; RefreshHandlers(); }
    }

    public Action<IndexSummaryMessage>? OnIndexSummary
    {
        get => _onIndexSummary;
        set { _onIndexSummary = value; RefreshHandlers(); }
    }

    /// <summary>An order changed status (<c>orderEvent</c>).</summary>
    public Action<OrderStatusMessage>? OnOrder
    {
        get => _onOrder;
        set { _onOrder = value; RefreshHandlers(); }
    }

    /// <summary>A fill of one of your orders (<c>orderMatchEvent</c>).</summary>
    public Action<OrderMatchMessage>? OnOrderMatch
    {
        get => _onOrderMatch;
        set { _onOrderMatch = value; RefreshHandlers(); }
    }

    /// <summary>Derivative position update (<c>clientPortfolioEvent</c>).</summary>
    public Action<PortfolioMessage>? OnPortfolio
    {
        get => _onPortfolio;
        set { _onPortfolio = value; RefreshHandlers(); }
    }

    /// <summary>connected / reconnected / disconnected / failed.</summary>
    public Action<ConnectionEvent>? OnConnection
    {
        get => _ws.OnState;
        set => _ws.OnState = value;
    }

    public void SetOnData(Action<object>? callback) => OnData = callback;

    public void SetOnTrading(Action<object>? callback) => OnTrading = callback;

    public void SetOnHeartbeat(Action<HeartbeatMessage>? callback) => OnHeartbeat = callback;

    public bool IsConnected => _ws.IsConnected;

    /// <summary>The error that ended the connection for good; null while healthy.</summary>
    public Exception? LastError => _ws.LastError;

    /// <summary>How many times the socket was re-opened after a drop.</summary>
    public int ReconnectCount => _ws.ReconnectCount;

    private static void Deliver<T>(Action<T> callback, T message)
    {
        try
        {
            callback(message);
        }
        catch (Exception ex)
        {
            Log.Error($"Error in a streaming callback: {ex.GetType().Name}");
        }
    }

    private void HandleData(JsonElement msg)
    {
        var message = StreamingMessageParser.ParseDataMessage(msg);
        if (_onData is { } all) Deliver(all, message);
        switch (message)
        {
            case MarketFlagMessage m when _onMarketFlag is { } cb: Deliver(cb, m); break;
            case IndexSummaryMessage m when _onIndexSummary is { } cb: Deliver(cb, m); break;
        }
    }

    private void HandleTrading(JsonElement msg)
    {
        var message = StreamingMessageParser.ParseTradingMessage(msg);
        if (_onTrading is { } all) Deliver(all, message);
        switch (message)
        {
            case OrderStatusMessage m when _onOrder is { } cb: Deliver(cb, m); break;
            case OrderMatchMessage m when _onOrderMatch is { } cb: Deliver(cb, m); break;
            case PortfolioMessage m when _onPortfolio is { } cb: Deliver(cb, m); break;
        }
    }

    /// <summary>Install (or clear) the one handler per channel from the callbacks currently set.</summary>
    private void RefreshHandlers()
    {
        lock (_lock)
        {
            var wantsData = _onData is not null || _onMarketFlag is not null || _onIndexSummary is not null;
            var wantsTrading = _onTrading is not null || _onOrder is not null || _onOrderMatch is not null || _onPortfolio is not null;
            _ws.On(StreamingChannel.Data, wantsData ? HandleData : null);
            _ws.On(StreamingChannel.Trading, wantsTrading ? HandleTrading : null);
        }
    }

    // ── sending ──────────────────────────────────────────────

    private Task SendAsync(
        string method, string channel, List<string> topics, CancellationToken ct, Action<JsonElement>? onResponse = null)
    {
        // Acknowledgements have their own route: registering on the request's channel would replace the data/trading handler.
        if (onResponse is not null) _ws.On(Constants.WsRouteAck, msg => onResponse(msg));
        return _ws.SendAsync(new WsRequest(method, channel, topics), ct);
    }

    private Task Data(string method, string topicType, string[] filters, CancellationToken ct, Action<JsonElement>? onResponse = null) =>
        SendAsync(method, StreamingChannel.Data, Topic.BuildMany(topicType, filters), ct, onResponse);

    // Topic types SubscribeBoard / SubscribeIndex cover; each goes out as <type>.<code>@*.
    private static readonly string[] GroupTopicTypes = ["trade", "quote", "room"];

    /// <summary>The per-type topics of an exchange / basket-index fan-out (<c>trade.VN30@*</c>); all validated before any is sent.</summary>
    internal static List<List<string>> BuildGroupTopics(string[] codes) =>
        GroupTopicTypes.Select(type => Topic.BuildMany(type, codes, Topic.StreamIntervalAll)).ToList();

    private async Task Group(string method, string[] codes, CancellationToken ct, Action<JsonElement>? onResponse = null)
    {
        foreach (var topics in BuildGroupTopics(codes))
            await SendAsync(method, StreamingChannel.Data, topics, ct, onResponse);
    }

    private Task Account(string method, string topicType, string accountNo, CancellationToken ct, Action<JsonElement>? onResponse = null) =>
        SendAsync(method, StreamingChannel.Trading,
            [Topic.Build(topicType, string.IsNullOrEmpty(accountNo) ? "*" : accountNo)], ct, onResponse);

    // ── ping ─────────────────────────────────────────────────

    /// <summary>Send one ping, or a ping every <paramref name="intervalMs"/> milliseconds until <see cref="StopPingLoop"/> / disconnect.</summary>
    public async Task PingAsync(int? intervalMs = null, CancellationToken ct = default, Action<JsonElement>? onResponse = null)
    {
        async Task SendOnceAsync(CancellationToken token)
        {
            Log.Debug("Sending ping to WebSocket server");
            await SendAsync(StreamingMethod.PingPong, StreamingChannel.Heartbeat, [], token, onResponse);
        }

        if (intervalMs is null)
        {
            await SendOnceAsync(ct);
            return;
        }
        if (intervalMs <= 0) throw new ValidationException("ping interval must be greater than 0 ms");

        StopPingLoop();
        var cts = new CancellationTokenSource();
        _pingCts = cts;
        var interval = TimeSpan.FromMilliseconds(intervalMs.Value);

        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await SendOnceAsync(cts.Token);
                    await Task.Delay(interval, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Ping loop cancelled");
            }
            catch (WebSocketException)
            {
                Log.Debug("Ping loop stopped: the connection is gone");
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ping loop: {ex.GetType().Name}");
            }
        }, CancellationToken.None);
    }

    public void StopPingLoop()
    {
        var cts = Interlocked.Exchange(ref _pingCts, null);
        if (cts is null) return;
        cts.Cancel();
        cts.Dispose();
    }

    // ── data subscriptions ───────────────────────────────────

    public Task SubscribeSymbolTradeAsync(string[] symbols, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        Data(StreamingMethod.Subscribe, "trade", symbols, ct, onResponse);

    public Task SubscribeSymbolQuoteAsync(string[] symbols, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        Data(StreamingMethod.Subscribe, "quote", symbols, ct, onResponse);

    public Task SubscribeSymbolRoomAsync(string[] symbols, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        Data(StreamingMethod.Subscribe, "room", symbols, ct, onResponse);

    public Task SubscribeSymbolPutThroughAsync(string[] symbols, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        Data(StreamingMethod.Subscribe, "put", symbols, ct, onResponse);

    public Task SubscribeSymbolOddLotAsync(string[] symbols, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        Data(StreamingMethod.Subscribe, "oddlot", symbols, ct, onResponse);

    /// <summary>Candles or ticks of symbols: <c>trade.SYMBOL@tick|1m|5m</c>.</summary>
    public Task SubscribeSymbolOhlcvAsync(string[] symbols, string interval, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SendAsync(StreamingMethod.Subscribe, StreamingChannel.Data, Topic.BuildMany("trade", symbols, interval), ct, onResponse);

    /// <summary>Master data (ceiling, floor, reference price) of symbols or boards.</summary>
    public Task SubscribeMarketAsync(string[] symbols, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        Data(StreamingMethod.Subscribe, "market", symbols, ct, onResponse);

    /// <summary>Session flags (ATO, LO, ATC, ...) of all boards.</summary>
    public Task SubscribeMarketFlagAsync(CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SendAsync(StreamingMethod.Subscribe, StreamingChannel.Data, [Constants.TopicMarketFlag], ct, onResponse);

    /// <summary>Ticks of the index itself (<c>trade.index.CODE</c>); pass the interval you will unsubscribe with.</summary>
    public Task SubscribeIndexTradeAsync(string[] indices, string? interval = null, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SendAsync(StreamingMethod.Subscribe, StreamingChannel.Data, Topic.BuildMany("trade.index", indices, interval), ct, onResponse);

    public Task SubscribeIndexSummaryAsync(string[] indices, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        Data(StreamingMethod.Subscribe, "indexsummary", indices, ct, onResponse);

    /// <summary>Trade + quote + foreign room of the symbols.</summary>
    public async Task SubscribeSymbolAsync(string[] symbols, CancellationToken ct = default, Action<JsonElement>? onResponse = null)
    {
        await SubscribeSymbolTradeAsync(symbols, ct, onResponse);
        await SubscribeSymbolQuoteAsync(symbols, ct, onResponse);
        await SubscribeSymbolRoomAsync(symbols, ct, onResponse);
    }

    /// <summary>Trade + quote + foreign room of an exchange's symbols, sent as <c>trade.HOSE@*</c> (etc.): the server only fans an exchange out with <c>@*</c>.</summary>
    public Task SubscribeBoardAsync(string[] boards, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        Group(StreamingMethod.Subscribe, boards, ct, onResponse);

    /// <summary>
    /// Trade + quote + foreign room of the symbols in an index basket, sent as <c>trade.VN30@*</c> (etc.). For the ticks of
    /// the index itself use <see cref="SubscribeIndexTradeAsync"/>.
    /// </summary>
    public Task SubscribeIndexAsync(string[] indices, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        Group(StreamingMethod.Subscribe, indices, ct, onResponse);

    // ── data unsubscriptions ─────────────────────────────────

    public Task UnsubscribeSymbolTradeAsync(string[] symbols, CancellationToken ct = default) =>
        Data(StreamingMethod.Unsubscribe, "trade", symbols, ct);

    public Task UnsubscribeSymbolQuoteAsync(string[] symbols, CancellationToken ct = default) =>
        Data(StreamingMethod.Unsubscribe, "quote", symbols, ct);

    public Task UnsubscribeSymbolRoomAsync(string[] symbols, CancellationToken ct = default) =>
        Data(StreamingMethod.Unsubscribe, "room", symbols, ct);

    public Task UnsubscribeSymbolPutThroughAsync(string[] symbols, CancellationToken ct = default) =>
        Data(StreamingMethod.Unsubscribe, "put", symbols, ct);

    public Task UnsubscribeSymbolOddLotAsync(string[] symbols, CancellationToken ct = default) =>
        Data(StreamingMethod.Unsubscribe, "oddlot", symbols, ct);

    public Task UnsubscribeSymbolOhlcvAsync(string[] symbols, string interval, CancellationToken ct = default) =>
        SendAsync(StreamingMethod.Unsubscribe, StreamingChannel.Data, Topic.BuildMany("trade", symbols, interval), ct);

    public Task UnsubscribeMarketAsync(string[] symbols, CancellationToken ct = default) =>
        Data(StreamingMethod.Unsubscribe, "market", symbols, ct);

    public Task UnsubscribeMarketFlagAsync(CancellationToken ct = default) =>
        SendAsync(StreamingMethod.Unsubscribe, StreamingChannel.Data, [Constants.TopicMarketFlag], ct);

    public Task UnsubscribeIndexTradeAsync(string[] indices, string? interval = null, CancellationToken ct = default) =>
        SendAsync(StreamingMethod.Unsubscribe, StreamingChannel.Data, Topic.BuildMany("trade.index", indices, interval), ct);

    public Task UnsubscribeIndexSummaryAsync(string[] indices, CancellationToken ct = default) =>
        Data(StreamingMethod.Unsubscribe, "indexsummary", indices, ct);

    public async Task UnsubscribeSymbolAsync(string[] symbols, CancellationToken ct = default)
    {
        await UnsubscribeSymbolTradeAsync(symbols, ct);
        await UnsubscribeSymbolQuoteAsync(symbols, ct);
        await UnsubscribeSymbolRoomAsync(symbols, ct);
    }

    public Task UnsubscribeBoardAsync(string[] boards, CancellationToken ct = default) =>
        Group(StreamingMethod.Unsubscribe, boards, ct);

    public Task UnsubscribeIndexAsync(string[] indices, CancellationToken ct = default) =>
        Group(StreamingMethod.Unsubscribe, indices, ct);

    // ── trading subscriptions ────────────────────────────────

    /// <summary>Order status and fills (<c>order.ACCOUNT</c>; "*" = every account).</summary>
    public Task SubscribeOrderStatusAsync(string accountNo = "*", CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        Account(StreamingMethod.Subscribe, "order", accountNo, ct, onResponse);

    public Task SubscribeFcoOrderStatusAsync(string accountNo = "*", CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SubscribeOrderStatusAsync(accountNo, ct, onResponse);

    public Task SubscribePortfolioAsync(string accountNo = "*", CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        Account(StreamingMethod.Subscribe, "portfolio", accountNo, ct, onResponse);

    public Task UnsubscribeOrderStatusAsync(string accountNo = "*", CancellationToken ct = default) =>
        Account(StreamingMethod.Unsubscribe, "order", accountNo, ct);

    public Task UnsubscribePortfolioAsync(string accountNo = "*", CancellationToken ct = default) =>
        Account(StreamingMethod.Unsubscribe, "portfolio", accountNo, ct);

    // ── what the server holds for this connection ────────────

    /// <summary>Topics this connection is subscribed to, as the server sees them (keys "trading" and "data").</summary>
    public async Task<Dictionary<string, List<string>>> ListSubscriptionAsync(int timeoutMs = 5000, CancellationToken ct = default)
    {
        var reply = new TaskCompletionSource<Dictionary<string, List<string>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        WebSocketClient.MessageHandler handler = msg => reply.TrySetResult(Dispatch.SplitSubscriptions(msg));
        _ws.On(Constants.WsRouteListSubscription, handler);
        try
        {
            await _ws.SendAsync(new WsRequest(StreamingMethod.ListSubscription), ct);
            return await reply.Task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), ct);
        }
        catch (TimeoutException ex)
        {
            throw new WebSocketException("Timed out waiting for the LIST_SUBSCRIPTION reply", ex);
        }
        finally
        {
            _ws.Off(Constants.WsRouteListSubscription, handler);
        }
    }

    /// <summary>Unsubscribe from everything the server lists for this connection; returns what was removed.</summary>
    public async Task<Dictionary<string, List<string>>> UnsubscribeAllAsync(CancellationToken ct = default)
    {
        var current = await ListSubscriptionAsync(ct: ct);
        foreach (var (key, channel) in new[] { ("data", StreamingChannel.Data), ("trading", StreamingChannel.Trading) })
        {
            if (current.TryGetValue(key, out var topics) && topics.Count > 0)
                await _ws.SendAsync(new WsRequest(StreamingMethod.Unsubscribe, channel, topics), ct);
        }
        return current;
    }

    public Task WaitAsync(int? timeoutMs = null) => _ws.WaitAsync(timeoutMs);

    // ── clearer names: same methods, the old names keep working ──

    public Task SubscribeOrdersAsync(string accountNo = "*", CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SubscribeOrderStatusAsync(accountNo, ct, onResponse);

    public Task SubscribeIndexTicksAsync(string[] indices, string? interval = null, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SubscribeIndexTradeAsync(indices, interval, ct, onResponse);

    public Task SubscribeMasterDataAsync(string[] symbols, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SubscribeMarketAsync(symbols, ct, onResponse);

    public Task SubscribeSessionFlagAsync(CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SubscribeMarketFlagAsync(ct, onResponse);

    public Task SubscribeTradesAsync(string[] symbols, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SubscribeSymbolTradeAsync(symbols, ct, onResponse);

    public Task SubscribeQuotesAsync(string[] symbols, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SubscribeSymbolQuoteAsync(symbols, ct, onResponse);

    public Task SubscribeForeignRoomAsync(string[] symbols, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SubscribeSymbolRoomAsync(symbols, ct, onResponse);

    public Task SubscribePutThroughAsync(string[] symbols, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SubscribeSymbolPutThroughAsync(symbols, ct, onResponse);

    public Task SubscribeOddLotAsync(string[] symbols, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SubscribeSymbolOddLotAsync(symbols, ct, onResponse);

    public Task SubscribeCandlesAsync(string[] symbols, string interval, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SubscribeSymbolOhlcvAsync(symbols, interval, ct, onResponse);

    public Task SubscribeExchangeTradesAsync(string[] boards, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SubscribeBoardAsync(boards, ct, onResponse);

    public Task SubscribeIndexConstituentsAsync(string[] indices, CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SubscribeIndexAsync(indices, ct, onResponse);

    public Task SubscribePositionsAsync(string accountNo = "*", CancellationToken ct = default, Action<JsonElement>? onResponse = null) =>
        SubscribePortfolioAsync(accountNo, ct, onResponse);

    public Task UnsubscribeOrdersAsync(string accountNo = "*", CancellationToken ct = default) =>
        UnsubscribeOrderStatusAsync(accountNo, ct);

    public Task UnsubscribeIndexTicksAsync(string[] indices, string? interval = null, CancellationToken ct = default) =>
        UnsubscribeIndexTradeAsync(indices, interval, ct);

    public Task UnsubscribeMasterDataAsync(string[] symbols, CancellationToken ct = default) =>
        UnsubscribeMarketAsync(symbols, ct);

    public Task UnsubscribeSessionFlagAsync(CancellationToken ct = default) =>
        UnsubscribeMarketFlagAsync(ct);

    public Task UnsubscribeTradesAsync(string[] symbols, CancellationToken ct = default) =>
        UnsubscribeSymbolTradeAsync(symbols, ct);

    public Task UnsubscribeQuotesAsync(string[] symbols, CancellationToken ct = default) =>
        UnsubscribeSymbolQuoteAsync(symbols, ct);

    public Task UnsubscribeForeignRoomAsync(string[] symbols, CancellationToken ct = default) =>
        UnsubscribeSymbolRoomAsync(symbols, ct);

    public Task UnsubscribePutThroughAsync(string[] symbols, CancellationToken ct = default) =>
        UnsubscribeSymbolPutThroughAsync(symbols, ct);

    public Task UnsubscribeOddLotAsync(string[] symbols, CancellationToken ct = default) =>
        UnsubscribeSymbolOddLotAsync(symbols, ct);

    public Task UnsubscribeCandlesAsync(string[] symbols, string interval, CancellationToken ct = default) =>
        UnsubscribeSymbolOhlcvAsync(symbols, interval, ct);

    public Task UnsubscribeExchangeTradesAsync(string[] boards, CancellationToken ct = default) =>
        UnsubscribeBoardAsync(boards, ct);

    public Task UnsubscribeIndexConstituentsAsync(string[] indices, CancellationToken ct = default) =>
        UnsubscribeIndexAsync(indices, ct);

    public Task UnsubscribePositionsAsync(string accountNo = "*", CancellationToken ct = default) =>
        UnsubscribePortfolioAsync(accountNo, ct);
}

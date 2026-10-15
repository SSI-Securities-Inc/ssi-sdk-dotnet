using System.Globalization;
using System.Text.Json;
using SsiSdk.Internal;
using SsiSdk.Models;
using SsiSdk.Services;

namespace SsiSdk.Experimental;

/// <summary>
/// EXPERIMENTAL: order entry and queries over the trading WebSocket. Same inputs and payloads as the REST
/// <see cref="TradingService"/>; each call returns the first response frame (the ack).
/// </summary>
public sealed class TradingWsService : IAsyncDisposable
{
    private static readonly JsonElement EmptyObject = JsonDocument.Parse("{}").RootElement;

    private readonly Config _config;

    public TradingWsClient Client { get; }

    public TradingWsService(TradingWsClient client, Config config)
    {
        Client = client;
        _config = config;
    }

    public Task CloseAsync() => Client.CloseAsync();

    public ValueTask DisposeAsync() => Client.DisposeAsync();

    private static JsonElement ResultOf(TradingWsResponse response) =>
        response.Result.ValueKind == JsonValueKind.Object ? response.Result : EmptyObject;

    private static object Num(decimal value) => Converter.ToNumber(Converter.Normalize(value));

    private Dictionary<string, object?> WithClientId(Dictionary<string, object?> parameters)
    {
        if (!string.IsNullOrEmpty(_config.ClientId)) parameters["clientId"] = _config.ClientId;
        return parameters;
    }

    // ── generic ─────────────────────────────────────────────────────────────

    /// <summary>Sends any command and returns its first response frame (no retry).</summary>
    public Task<TradingWsResponse> CommandAsync(string method, object? parameters, CancellationToken ct = default) =>
        Client.RequestAsync(method, parameters, ct);

    // ── orders ──────────────────────────────────────────────────────────────

    public async Task<PlaceOrderResponse> PlaceOrderAsync(
        string accountNo, string symbol, string side, int quantity, decimal? price, string orderType,
        string? clientRequestId = null, CancellationToken ct = default)
    {
        if (price is not null) Validate.RequireNonNegative(price.Value, "price");
        var payload = TradingService.BuildPlaceOrder(_config, accountNo, symbol, side, quantity, price, orderType, clientRequestId);
        var response = await Client.RequestAsync("order.place", payload, ct).ConfigureAwait(false);
        return PlaceOrderResponse.FromJson(ResultOf(response));
    }

    /// <summary>Changes the price or the quantity (not both) of one order.</summary>
    public async Task<ModifyOrderResponse> AmendOrderAsync(
        string accountNo, decimal? price = null, int? quantity = null, string? orderId = null,
        string? clientRequestId = null, CancellationToken ct = default)
    {
        var payload = TradingService.BuildModifyOrder(_config, accountNo, orderId, clientRequestId, price, quantity);
        var response = await Client.RequestAsync("order.amend", payload, ct).ConfigureAwait(false);
        return ModifyOrderResponse.FromJson(ResultOf(response));
    }

    public async Task<CancelOrderResponse> CancelOrderAsync(
        string accountNo, string? orderId = null, string? clientRequestId = null, CancellationToken ct = default)
    {
        var payload = TradingService.BuildCancelOrder(_config, accountNo, orderId, clientRequestId);
        var response = await Client.RequestAsync("order.cancel", payload, ct).ConfigureAwait(false);
        return CancelOrderResponse.FromJson(ResultOf(response));
    }

    public Task<TradingWsResponse> BatchNewOrdersAsync(IReadOnlyCollection<BatchPlaceOrderItem> orders, CancellationToken ct = default) =>
        Client.RequestAsync("order.batchNew", TradingService.BuildBatchPlace(_config, orders), ct);

    public Task<TradingWsResponse> BatchCancelOrdersAsync(IReadOnlyCollection<BatchCancelOrderItem> orders, CancellationToken ct = default) =>
        Client.RequestAsync("order.batchCancel", TradingService.BuildBatchCancel(_config, orders), ct);

    // ── FCO ─────────────────────────────────────────────────────────────────

    private async Task<FCOPlaceResponse> PlaceFcoAsync(Dictionary<string, object?> payload, CancellationToken ct)
    {
        var response = await Client.RequestAsync("fco.place", payload, ct).ConfigureAwait(false);
        return FCOPlaceResponse.FromJson(ResultOf(response));
    }

    public Task<FCOPlaceResponse> PlaceFcoGtdAsync(
        string accountNo, string symbol, string side, int quantity, FcoPrice price, decimal priceSlip,
        string fromDate, string toDate, string? code = null, CancellationToken ct = default) =>
        PlaceFcoAsync(TradingService.BuildFcoGtd(_config, accountNo, symbol, side, quantity, price, priceSlip, fromDate, toDate, code), ct);

    public Task<FCOPlaceResponse> PlaceFcoStopAsync(
        string accountNo, string symbol, string side, int quantity, decimal stopPrice, string operatorType,
        string fromDate, string toDate, string? code = null, CancellationToken ct = default) =>
        PlaceFcoAsync(TradingService.BuildFcoStop(_config, accountNo, symbol, side, quantity, stopPrice, operatorType, fromDate, toDate, code), ct);

    public Task<FCOPlaceResponse> PlaceFcoStopLimitAsync(
        string accountNo, string symbol, string side, int quantity, FcoPrice price, decimal priceSlip, decimal stopPrice, string operatorType,
        string fromDate, string toDate, string? code = null, CancellationToken ct = default) =>
        PlaceFcoAsync(TradingService.BuildFcoStopLimit(_config, accountNo, symbol, side, quantity, price, priceSlip, stopPrice, operatorType, fromDate, toDate, code), ct);

    public Task<FCOPlaceResponse> PlaceFcoTrailingStopAsync(
        string accountNo, string symbol, string side, int quantity, decimal activePrice, decimal trailingAmount,
        string fromDate, string toDate, string? code = null, CancellationToken ct = default) =>
        PlaceFcoAsync(TradingService.BuildFcoTrailingStop(_config, accountNo, symbol, side, quantity, activePrice, trailingAmount, fromDate, toDate, code), ct);

    public Task<FCOPlaceResponse> PlaceFcoTrailingStopLimitAsync(
        string accountNo, string symbol, string side, int quantity, decimal activePrice, decimal trailingAmount, decimal priceSlip,
        string fromDate, string toDate, string? code = null, CancellationToken ct = default) =>
        PlaceFcoAsync(TradingService.BuildFcoTrailingStopLimit(_config, accountNo, symbol, side, quantity, activePrice, trailingAmount, priceSlip, fromDate, toDate, code), ct);

    public Task<FCOPlaceResponse> PlaceFcoOcoAsync(
        string accountNo, string symbol, string side, int quantity, decimal tpActivePrice, decimal slActivePrice,
        FcoPrice tpPrice, FcoPrice slPrice, decimal tpSlip, decimal slSlip,
        string fromDate, string toDate, string? code = null, CancellationToken ct = default) =>
        PlaceFcoAsync(TradingService.BuildFcoOco(_config, accountNo, symbol, side, quantity, tpActivePrice, slActivePrice, tpPrice, slPrice, tpSlip, slSlip, fromDate, toDate, code), ct);

    public Task<FCOPlaceResponse> PlaceFcoBullBearAsync(
        string accountNo, string symbol, string side, int quantity, FcoPrice price, decimal priceSlip,
        decimal tpActivePrice, decimal slActivePrice, FcoPrice tpPrice, FcoPrice slPrice, decimal tpSlip, decimal slSlip,
        string fromDate, string toDate, string? code = null, CancellationToken ct = default) =>
        PlaceFcoAsync(TradingService.BuildFcoBullBear(_config, accountNo, symbol, side, quantity, price, priceSlip, tpActivePrice, slActivePrice, tpPrice, slPrice, tpSlip, slSlip, fromDate, toDate, code), ct);

    /// <summary>Cancels a conditional order (<c>fco.cancel</c>).</summary>
    public async Task<FCOCancelResponse> CancelFcoAsync(string fcoId, string? code = null, CancellationToken ct = default)
    {
        var response = await Client.RequestAsync("fco.cancel", TradingService.BuildFcoCancel(_config, fcoId, code), ct).ConfigureAwait(false);
        return FCOCancelResponse.FromJson(ResultOf(response));
    }

    /// <summary>Lists conditional orders (<c>fco.list</c>); takes a single process status, not a list.</summary>
    public Task<TradingWsResponse> ListFcoAsync(
        string accountNo, string? fcoId = null, string? type = null, string? processStatus = null, string? symbol = null,
        string? side = null, string? fromDate = null, string? toDate = null, int? pageIndex = null, int? pageSize = null,
        CancellationToken ct = default)
    {
        var query = TradingService.BuildFcoList(
            accountNo, fcoId, type, processStatus is null ? null : [processStatus], symbol, side, fromDate, toDate, pageIndex, pageSize);
        var parameters = new Dictionary<string, object?>();
        foreach (var (key, value) in query)
            parameters[key] = key is "pageIndex" or "pageSize" ? int.Parse(value, CultureInfo.InvariantCulture) : value;
        return Client.RequestAsync("fco.list", parameters, ct);
    }

    public Task<TradingWsResponse> FcoOrderBookAsync(string fcoId, int? pageIndex = null, int? pageSize = null, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(fcoId, "fcoId");
        var parameters = new Dictionary<string, object?>
        {
            ["fcoId"] = fcoId,
            ["pageIndex"] = pageIndex ?? Constants.DefaultPage,
            ["pageSize"] = pageSize ?? 10,
        };
        return Client.RequestAsync("fco.orderbook", parameters, ct);
    }

    public Task<TradingWsResponse> FcoStatusHistoryAsync(string fcoId, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(fcoId, "fcoId");
        return Client.RequestAsync("fco.statusHistory", new Dictionary<string, object?> { ["fcoId"] = fcoId }, ct);
    }

    // ── event subscriptions ─────────────────────────────────────────────────

    /// <summary>The server refuses wildcards and account lists on this socket: one account per call.</summary>
    private static string AccountTopic(string kind, string accountNo)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        if (accountNo.Contains('*') || accountNo.Contains(','))
            throw new ValidationException("subscribe to one account at a time (no wildcard or list)");
        return Topic.Build(kind, accountNo);
    }

    public Task SubscribeOrderEventsAsync(string accountNo, CancellationToken ct = default) =>
        Client.SubscribeAsync("TRADING", [AccountTopic("order", accountNo)], ct);

    public Task UnsubscribeOrderEventsAsync(string accountNo, CancellationToken ct = default) =>
        Client.UnsubscribeAsync("TRADING", [AccountTopic("order", accountNo)], ct);

    public Task SubscribePortfolioEventsAsync(string accountNo, CancellationToken ct = default) =>
        Client.SubscribeAsync("TRADING", [AccountTopic("portfolio", accountNo)], ct);

    public Task UnsubscribePortfolioEventsAsync(string accountNo, CancellationToken ct = default) =>
        Client.UnsubscribeAsync("TRADING", [AccountTopic("portfolio", accountNo)], ct);

    /// <summary>Drops every trading subscription of this socket (UNSUBSCRIBE with no topics).</summary>
    public Task UnsubscribeAllEventsAsync(CancellationToken ct = default) =>
        Client.UnsubscribeAsync("TRADING", [], ct);

    // ── queries ─────────────────────────────────────────────────────────────

    public Task<TradingWsResponse> QueryPositionAsync(string accountNo, bool querySummary = true, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var parameters = WithClientId(new Dictionary<string, object?> { ["accountNo"] = accountNo, ["querySummary"] = querySummary });
        return Client.RequestAsync("query.position", parameters, ct);
    }

    public Task<TradingWsResponse> QueryMaxBuySellAsync(string accountNo, string symbol, decimal? price = null, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");
        var parameters = new Dictionary<string, object?> { ["accountNo"] = accountNo, ["symbol"] = symbol.ToUpperInvariant() };
        if (price is not null)
        {
            Validate.RequireNonNegative(price.Value, "price");
            parameters["price"] = Num(price.Value);
        }
        return Client.RequestAsync("query.maxBuySell", parameters, ct);
    }

    public Task<TradingWsResponse> QueryAccountBalanceAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        return Client.RequestAsync("query.accountBalance", WithClientId(new Dictionary<string, object?> { ["accountNo"] = accountNo }), ct);
    }

    public Task<TradingWsResponse> QueryPpmmrAccountAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        return Client.RequestAsync("query.ppmmrAccount", new Dictionary<string, object?> { ["accountNo"] = accountNo }, ct);
    }

    public Task<TradingWsResponse> QueryOrderBookAsync(
        string accountNo, string? fromDate = null, string? toDate = null, string? symbol = null, string? orderStatus = null,
        int? pageIndex = null, int? pageSize = null, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var parameters = new Dictionary<string, object?> { ["accountNo"] = accountNo };
        if (fromDate is not null && toDate is not null) Validate.RequireDateRange(fromDate, toDate);
        if (fromDate is not null) { Validate.ParseDateArg(fromDate, "from"); parameters["from"] = fromDate; }
        if (toDate is not null) { Validate.ParseDateArg(toDate, "to"); parameters["to"] = toDate; }
        if (symbol is not null) parameters["symbol"] = symbol;
        if (orderStatus is not null) parameters["orderStatus"] = orderStatus;
        if (pageIndex is not null) parameters["pageIndex"] = pageIndex.Value;
        if (pageSize is not null) parameters["pageSize"] = pageSize.Value;
        return Client.RequestAsync("query.orderBook", parameters, ct);
    }
}

/// <summary>Factory for the experimental trading WebSocket.</summary>
public static class TradingWs
{
    /// <summary>Copy of the options with the request timeout and user agent taken from the config where the caller left them unset.</summary>
    internal static TradingWsOptions MergeOptions(Config config, TradingWsOptions? options)
    {
        var merged = options?.Clone() ?? new TradingWsOptions();
        merged.RequestTimeoutMs ??= config.TimeoutMs > 0 ? config.TimeoutMs : 10_000;
        if (string.IsNullOrWhiteSpace(merged.UserAgent)) merged.UserAgent = config.UserAgent;
        return merged;
    }

    /// <summary>Opens the socket with the current access token (authenticate first) and returns the service.</summary>
    public static async Task<TradingWsService> ConnectAsync(AuthClient auth, TradingWsOptions? options = null, CancellationToken ct = default)
    {
        var config = auth.Config;
        options = MergeOptions(config, options);
        var client = new TradingWsClient(config, token => auth.EnsureAuthenticatedAsync(ct: token), options);
        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            await client.CloseAsync().ConfigureAwait(false);
            throw;
        }
        return new TradingWsService(client, config);
    }
}

using System.Globalization;
using System.Text.Json;
using SsiSdk.Internal;
using SsiSdk.Models;
using SsiSdk.Transport;

namespace SsiSdk.Services;

/// <summary>Orders, conditional orders (FCO) and max buy/sell. Every mutating call is signed with <c>Config.PrivateKey</c>.</summary>
public sealed class TradingService
{
    private static readonly string DefaultUserAgent = $"SSI .NET SDK/{SdkVersion.Value}";
    private static readonly HashSet<string> PricedOrderTypes = [OrderType.LO, OrderType.PLO];

    private readonly RestClient _rest;

    internal TradingService(RestClient rest)
    {
        _rest = rest;
    }

    private Config Config => _rest.Config;

    // ── payload building (internal for tests) ───────────────────────────────

    /// <summary>Device id and user agent every order payload takes from this machine and <see cref="Config"/>.</summary>
    private static void AddIdentity(Dictionary<string, object?> payload, Config config)
    {
        payload["deviceId"] = DeviceId.Get();
        payload["userAgent"] = string.IsNullOrWhiteSpace(config.UserAgent) ? DefaultUserAgent : config.UserAgent;
    }

    /// <summary>The caller's idempotency key, or a random one (at most 20 characters).</summary>
    internal static string ClientRequestIdOrNew(string? value)
    {
        if (value is null) return IdGenerator.GenerateRequestId();
        Validate.RequireNonEmpty(value, "clientRequestId");
        if (value.Length > Constants.MaxClientRequestIdLength)
            throw new ValidationException($"clientRequestId must be at most {Constants.MaxClientRequestIdLength} characters");
        return value;
    }

    internal static Dictionary<string, object?> BuildPlaceOrder(
        Config config, string accountNo, string symbol, string side, int quantity, decimal? price, string orderType,
        string? clientRequestId = null)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequireNonEmpty(side, "side");
        Validate.RequireNonEmpty(orderType, "orderType");
        Validate.RequirePositive(quantity, "quantity");
        if (price is not null) Validate.RequireNonNegative(price.Value, "price");
        if (price is null && PricedOrderTypes.Contains(orderType))
            throw new ValidationException($"price is required for {orderType} orders");

        var payload = new Dictionary<string, object?>
        {
            ["accountNo"] = accountNo,
            ["symbol"] = symbol,
            ["side"] = side,
            ["quantity"] = quantity,
            // Market-priced types (ATO/ATC/MTL/MP/...) go out with price 0.
            ["price"] = price is null ? "0" : Converter.FormatPrice(price.Value),
            ["orderType"] = orderType,
            ["clientRequestId"] = ClientRequestIdOrNew(clientRequestId),
        };
        AddIdentity(payload, config);
        return payload;
    }

    internal static Dictionary<string, object?> BuildModifyOrder(
        Config config, string accountNo, string? orderId, string? clientRequestId, decimal? price, int? quantity)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireExactlyOne(!string.IsNullOrEmpty(orderId), !string.IsNullOrEmpty(clientRequestId), "orderId", "clientRequestId");
        if (price is null && quantity is null)
            throw new ValidationException("either price or quantity is required");
        if (price is not null && quantity is not null)
            throw new ValidationException("price and quantity cannot be modified in the same request");
        if (price is not null) Validate.RequirePositive(price.Value, "price");
        if (quantity is not null) Validate.RequirePositive(quantity.Value, "quantity");

        var payload = new Dictionary<string, object?>
        {
            ["accountNo"] = accountNo,
            ["clientModifyId"] = IdGenerator.GenerateRequestId(),
        };
        AddIdentity(payload, config);
        if (!string.IsNullOrEmpty(orderId)) payload["orderId"] = orderId;
        if (!string.IsNullOrEmpty(clientRequestId)) payload["clientRequestId"] = clientRequestId;
        if (quantity is not null) payload["quantity"] = quantity.Value;
        if (price is not null) payload["price"] = Converter.FormatPrice(price.Value);
        return payload;
    }

    internal static Dictionary<string, object?> BuildCancelOrder(Config config, string accountNo, string? orderId, string? clientRequestId)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireExactlyOne(!string.IsNullOrEmpty(orderId), !string.IsNullOrEmpty(clientRequestId), "orderId", "clientRequestId");
        var payload = new Dictionary<string, object?>
        {
            ["accountNo"] = accountNo,
            ["clientCancelId"] = IdGenerator.GenerateRequestId(),
        };
        AddIdentity(payload, config);
        if (!string.IsNullOrEmpty(orderId)) payload["orderId"] = orderId;
        if (!string.IsNullOrEmpty(clientRequestId)) payload["clientRequestId"] = clientRequestId;
        return payload;
    }

    /// <summary>A batch holds 1..<paramref name="limit"/> orders (<c>Config.MaxBatchOrders</c>; the server default is 20).</summary>
    internal static IReadOnlyList<T> CheckBatch<T>(IReadOnlyCollection<T>? orders, int limit = Constants.MaxBatchOrders)
    {
        if (orders is null || orders.Count == 0)
            throw new ValidationException("a batch needs at least one order");
        if (orders.Count > limit)
            throw new ValidationException($"a batch holds at most {limit} orders, got {orders.Count}");
        return orders.ToList();
    }

    /// <summary>Envelope with a fresh id (a reused one is a 409) and the current time in epoch milliseconds.</summary>
    internal static Dictionary<string, object?> BatchEnvelope(List<Dictionary<string, object?>> orders) => new()
    {
        ["batchRequestId"] = IdGenerator.GenerateRequestId(),
        ["batchRequestTime"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        ["orders"] = orders,
    };

    /// <summary>Validates every order first (one bad order rejects the whole batch), then builds the body.</summary>
    internal static Dictionary<string, object?> BuildBatchPlace(Config config, IReadOnlyCollection<BatchPlaceOrderItem>? items)
    {
        var built = CheckBatch(items, config.MaxBatchOrders)
            .Select(i => BuildPlaceOrder(config, i.AccountNo, i.Symbol, i.Side, i.Quantity, i.Price, i.OrderType, i.ClientRequestId))
            .ToList();
        return BatchEnvelope(built);
    }

    internal static Dictionary<string, object?> BuildBatchCancel(Config config, IReadOnlyCollection<BatchCancelOrderItem>? items)
    {
        var built = CheckBatch(items, config.MaxBatchOrders)
            .Select(i => BuildCancelOrder(config, i.AccountNo, i.OrderId, i.ClientRequestId))
            .ToList();
        return BatchEnvelope(built);
    }

    internal static List<KeyValuePair<string, string>> BuildMaxBuySell(string accountNo, string symbol, decimal? price)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");
        var query = new List<KeyValuePair<string, string>>
        {
            new("accountNo", accountNo),
            new("symbol", symbol.ToUpperInvariant()),
        };
        if (price is not null) query.Add(new("price", Converter.FormatPrice(price.Value)));
        return query;
    }

    // ── signing ─────────────────────────────────────────────────────────────

    /// <summary>Serializes the payload once; the same text is signed and sent.</summary>
    private (string Json, Dictionary<string, string> Headers) Sign(Dictionary<string, object?> payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var signature = Crypto.Sign(json, Config.PrivateKey);
        return (json, new Dictionary<string, string> { [Constants.HeaderSignature] = signature });
    }

    // ── orders ──────────────────────────────────────────────────────────────

    public async Task<PlaceOrderResponse> PlaceOrderAsync(
        string accountNo, string symbol, string side, int quantity, decimal? price, string orderType,
        string? clientRequestId = null, CancellationToken ct = default)
    {
        var payload = BuildPlaceOrder(Config, accountNo, symbol, side, quantity, price, orderType, clientRequestId);
        var (json, headers) = Sign(payload);
        var data = await _rest.PostAsync(Constants.EpTradingOrder, json, headers, ct).ConfigureAwait(false);
        return PlaceOrderResponse.FromJson(Converter.Payload(data));
    }

    public Task<PlaceOrderResponse> PlaceLimitOrderAsync(
        string accountNo, string symbol, string side, int quantity, decimal price,
        string? clientRequestId = null, CancellationToken ct = default)
    {
        Validate.RequirePositive(price, "price");
        return PlaceOrderAsync(accountNo, symbol, side, quantity, price, OrderType.LO, clientRequestId, ct);
    }

    public Task<PlaceOrderResponse> PlaceMarketOrderAsync(
        string accountNo, string symbol, string side, int quantity, string? clientRequestId = null, CancellationToken ct = default) =>
        PlaceOrderAsync(accountNo, symbol, side, quantity, 0m, OrderType.MTL, clientRequestId, ct);

    public Task<PlaceOrderResponse> PlaceAtoOrderAsync(
        string accountNo, string symbol, string side, int quantity, string? clientRequestId = null, CancellationToken ct = default) =>
        PlaceOrderAsync(accountNo, symbol, side, quantity, 0m, OrderType.ATO, clientRequestId, ct);

    public Task<PlaceOrderResponse> PlaceAtcOrderAsync(
        string accountNo, string symbol, string side, int quantity, string? clientRequestId = null, CancellationToken ct = default) =>
        PlaceOrderAsync(accountNo, symbol, side, quantity, 0m, OrderType.ATC, clientRequestId, ct);

    private async Task<ModifyOrderResponse> ModifyOrderAsync(
        string accountNo, string? orderId, string? clientRequestId, decimal? price, int? quantity, CancellationToken ct)
    {
        var payload = BuildModifyOrder(Config, accountNo, orderId, clientRequestId, price, quantity);
        var (json, headers) = Sign(payload);
        var data = await _rest.PutAsync(Constants.EpTradingOrder, json, headers, ct).ConfigureAwait(false);
        return ModifyOrderResponse.FromJson(Converter.Payload(data));
    }

    public Task<ModifyOrderResponse> ModifyOrderPriceAsync(string accountNo, string clientRequestId, decimal price, CancellationToken ct = default)
    {
        Validate.RequirePositive(price, "price");
        return ModifyOrderAsync(accountNo, null, clientRequestId, price, null, ct);
    }

    public Task<ModifyOrderResponse> ModifyOrderPriceByOrderIdAsync(string accountNo, string orderId, decimal price, CancellationToken ct = default)
    {
        Validate.RequirePositive(price, "price");
        return ModifyOrderAsync(accountNo, orderId, null, price, null, ct);
    }

    public Task<ModifyOrderResponse> ModifyOrderQuantityAsync(string accountNo, string clientRequestId, int quantity, CancellationToken ct = default) =>
        ModifyOrderAsync(accountNo, null, clientRequestId, null, quantity, ct);

    public Task<ModifyOrderResponse> ModifyOrderQuantityByOrderIdAsync(string accountNo, string orderId, int quantity, CancellationToken ct = default) =>
        ModifyOrderAsync(accountNo, orderId, null, null, quantity, ct);

    private async Task<CancelOrderResponse> CancelOrderInternalAsync(
        string accountNo, string? orderId, string? clientRequestId, CancellationToken ct)
    {
        var payload = BuildCancelOrder(Config, accountNo, orderId, clientRequestId);
        var (json, headers) = Sign(payload);
        var data = await _rest.DeleteAsync(Constants.EpTradingOrder, json, headers, ct).ConfigureAwait(false);
        return CancelOrderResponse.FromJson(Converter.Payload(data));
    }

    public Task<CancelOrderResponse> CancelOrderAsync(string accountNo, string clientRequestId, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(clientRequestId, "clientRequestId");
        return CancelOrderInternalAsync(accountNo, null, clientRequestId, ct);
    }

    public Task<CancelOrderResponse> CancelOrderByOrderIdAsync(string accountNo, string orderId, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(orderId, "orderId");
        return CancelOrderInternalAsync(accountNo, orderId, null, ct);
    }

    /// <summary>Places up to <c>Config.MaxBatchOrders</c> orders in one signed request (<c>POST /order/batch</c>).</summary>
    public async Task<BatchOrderResponse> PlaceBatchOrdersAsync(IReadOnlyCollection<BatchPlaceOrderItem> orders, CancellationToken ct = default)
    {
        var (json, headers) = Sign(BuildBatchPlace(Config, orders));
        var data = await _rest.PostAsync(Constants.EpTradingOrderBatch, json, headers, ct).ConfigureAwait(false);
        return BatchOrderResponse.FromJson(data);
    }

    /// <summary>Cancels up to <c>Config.MaxBatchOrders</c> orders in one signed request (<c>DELETE /order/batch</c>).</summary>
    public async Task<BatchOrderResponse> CancelBatchOrdersAsync(IReadOnlyCollection<BatchCancelOrderItem> orders, CancellationToken ct = default)
    {
        var (json, headers) = Sign(BuildBatchCancel(Config, orders));
        var data = await _rest.DeleteAsync(Constants.EpTradingOrderBatch, json, headers, ct).ConfigureAwait(false);
        return BatchOrderResponse.FromJson(data);
    }

    // ── max buy / sell ──────────────────────────────────────────────────────

    public async Task<MaxBuySellResponse> GetMaxBuySellAsync(string accountNo, string symbol, decimal price, CancellationToken ct = default)
    {
        Validate.RequirePositive(price, "price");
        var data = await _rest.GetAsync(Constants.EpTradingMaxBuySell, BuildMaxBuySell(accountNo, symbol, price), ct: ct).ConfigureAwait(false);
        return MaxBuySellResponse.FromJson(Converter.Payload(data), symbol);
    }

    public async Task<MaxBuySellResponse> GetMaxBuySellAtMarketPriceAsync(string accountNo, string symbol, CancellationToken ct = default)
    {
        var data = await _rest.GetAsync(Constants.EpTradingMaxBuySell, BuildMaxBuySell(accountNo, symbol, null), ct: ct).ConfigureAwait(false);
        return MaxBuySellResponse.FromJson(Converter.Payload(data), symbol);
    }

    // ── Flexible Conditional Orders (FCO) ───────────────────────────────────

    private static object Num(decimal value) => Converter.ToNumber(Converter.Normalize(value));

    /// <summary>
    /// Validates the order window (both dates <c>yyyy/MM/dd HH:mm:ss</c>, at most 31 days) and starts the payload.
    /// <c>deviceId</c> is always this machine's id, whatever the caller set.
    /// </summary>
    internal static Dictionary<string, object?> FcoBase(
        Config config, string type, string accountNo, string symbol, string side, int quantity, string? fromDate, string? toDate)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequireNonEmpty(side, "side");
        if (fromDate is not null && toDate is not null)
            Validate.RequireDateRange(fromDate, toDate, timeRequired: true, maxDays: Constants.MaxFcoWindowDays);
        else
        {
            if (fromDate is not null) Validate.ParseDateArg(fromDate, "from", timeRequired: true);
            if (toDate is not null) Validate.ParseDateArg(toDate, "to", timeRequired: true);
        }
        var payload = new Dictionary<string, object?>
        {
            ["accountNo"] = accountNo,
            ["type"] = type,
            ["symbol"] = symbol,
            ["side"] = side,
            ["quantity"] = quantity,
            ["from"] = fromDate,
            ["to"] = toDate,
        };
        AddIdentity(payload, config);
        return payload;
    }

    private static void AddCode(Dictionary<string, object?> payload, string? code)
    {
        if (!string.IsNullOrEmpty(code)) payload["code"] = code;
    }

    private async Task<FCOPlaceResponse> PlaceFcoAsync(Dictionary<string, object?> payload, CancellationToken ct)
    {
        var (json, headers) = Sign(payload);
        var data = await _rest.PostAsync(Constants.EpTradingFcoOrder, json, headers, ct).ConfigureAwait(false);
        return FCOPlaceResponse.FromJson(data);
    }

    /// <summary>Good-till-date order. <paramref name="price"/> is a number or an order-type word (then no slip).</summary>
    public Task<FCOPlaceResponse> PlaceFcoGtdAsync(
        string accountNo, string symbol, string side, int quantity, FcoPrice price, decimal priceSlip,
        string fromDate, string toDate, string? code = null, CancellationToken ct = default) =>
        PlaceFcoAsync(BuildFcoGtd(Config, accountNo, symbol, side, quantity, price, priceSlip, fromDate, toDate, code), ct);

    internal static Dictionary<string, object?> BuildFcoGtd(
        Config config, string accountNo, string symbol, string side, int quantity, FcoPrice price, decimal priceSlip,
        string fromDate, string toDate, string? code)
    {
        var payload = FcoBase(config, FCOType.GTD, accountNo, symbol, side, quantity, fromDate, toDate);
        var (text, keepSlip) = price.Resolve();
        payload["price"] = text;
        payload["priceSlip"] = keepSlip ? Num(priceSlip) : 0;
        AddCode(payload, code);
        return payload;
    }

    public Task<FCOPlaceResponse> PlaceFcoStopAsync(
        string accountNo, string symbol, string side, int quantity, decimal stopPrice, string operatorType,
        string fromDate, string toDate, string? code = null, CancellationToken ct = default) =>
        PlaceFcoAsync(BuildFcoStop(Config, accountNo, symbol, side, quantity, stopPrice, operatorType, fromDate, toDate, code), ct);

    internal static Dictionary<string, object?> BuildFcoStop(
        Config config, string accountNo, string symbol, string side, int quantity, decimal stopPrice, string operatorType,
        string fromDate, string toDate, string? code)
    {
        var op = FcoOperators.Require(operatorType);
        var payload = FcoBase(config, FCOType.Stop, accountNo, symbol, side, quantity, fromDate, toDate);
        payload["price"] = OrderType.MTL;
        payload["priceSlip"] = 0;
        payload["stopPrice"] = Num(stopPrice);
        payload["operator"] = op;
        AddCode(payload, code);
        return payload;
    }

    public Task<FCOPlaceResponse> PlaceFcoStopLimitAsync(
        string accountNo, string symbol, string side, int quantity, FcoPrice price, decimal priceSlip, decimal stopPrice, string operatorType,
        string fromDate, string toDate, string? code = null, CancellationToken ct = default) =>
        PlaceFcoAsync(BuildFcoStopLimit(Config, accountNo, symbol, side, quantity, price, priceSlip, stopPrice, operatorType, fromDate, toDate, code), ct);

    internal static Dictionary<string, object?> BuildFcoStopLimit(
        Config config, string accountNo, string symbol, string side, int quantity, FcoPrice price, decimal priceSlip, decimal stopPrice,
        string operatorType, string fromDate, string toDate, string? code)
    {
        var op = FcoOperators.Require(operatorType);
        var payload = FcoBase(config, FCOType.StopLimit, accountNo, symbol, side, quantity, fromDate, toDate);
        payload["price"] = price.Resolve().Text;
        payload["priceSlip"] = Num(priceSlip);
        payload["stopPrice"] = Num(stopPrice);
        payload["operator"] = op;
        AddCode(payload, code);
        return payload;
    }

    public Task<FCOPlaceResponse> PlaceFcoTrailingStopAsync(
        string accountNo, string symbol, string side, int quantity, decimal activePrice, decimal trailingAmount,
        string fromDate, string toDate, string? code = null, CancellationToken ct = default) =>
        PlaceFcoAsync(BuildFcoTrailingStop(Config, accountNo, symbol, side, quantity, activePrice, trailingAmount, fromDate, toDate, code), ct);

    internal static Dictionary<string, object?> BuildFcoTrailingStop(
        Config config, string accountNo, string symbol, string side, int quantity, decimal activePrice, decimal trailingAmount,
        string fromDate, string toDate, string? code)
    {
        var payload = FcoBase(config, FCOType.TrailingStop, accountNo, symbol, side, quantity, fromDate, toDate);
        payload["activePrice"] = Num(activePrice);
        payload["trailingAmount"] = Num(trailingAmount);
        payload["price"] = OrderType.MTL;
        payload["priceSlip"] = 0;
        AddCode(payload, code);
        return payload;
    }

    public Task<FCOPlaceResponse> PlaceFcoTrailingStopLimitAsync(
        string accountNo, string symbol, string side, int quantity, decimal activePrice, decimal trailingAmount, decimal priceSlip,
        string fromDate, string toDate, string? code = null, CancellationToken ct = default) =>
        PlaceFcoAsync(BuildFcoTrailingStopLimit(Config, accountNo, symbol, side, quantity, activePrice, trailingAmount, priceSlip, fromDate, toDate, code), ct);

    internal static Dictionary<string, object?> BuildFcoTrailingStopLimit(
        Config config, string accountNo, string symbol, string side, int quantity, decimal activePrice, decimal trailingAmount, decimal priceSlip,
        string fromDate, string toDate, string? code)
    {
        var payload = FcoBase(config, FCOType.TrailingStopLimit, accountNo, symbol, side, quantity, fromDate, toDate);
        payload["activePrice"] = Num(activePrice);
        payload["trailingAmount"] = Num(trailingAmount);
        payload["priceSlip"] = Num(priceSlip);
        AddCode(payload, code);
        return payload;
    }

    public Task<FCOPlaceResponse> PlaceFcoOcoAsync(
        string accountNo, string symbol, string side, int quantity, decimal tpActivePrice, decimal slActivePrice,
        FcoPrice tpPrice, FcoPrice slPrice, decimal tpSlip, decimal slSlip,
        string fromDate, string toDate, string? code = null, CancellationToken ct = default) =>
        PlaceFcoAsync(BuildFcoOco(Config, accountNo, symbol, side, quantity, tpActivePrice, slActivePrice, tpPrice, slPrice, tpSlip, slSlip, fromDate, toDate, code), ct);

    internal static Dictionary<string, object?> BuildFcoOco(
        Config config, string accountNo, string symbol, string side, int quantity, decimal tpActivePrice, decimal slActivePrice,
        FcoPrice tpPrice, FcoPrice slPrice, decimal tpSlip, decimal slSlip, string fromDate, string toDate, string? code)
    {
        var payload = FcoBase(config, FCOType.OCO, accountNo, symbol, side, quantity, fromDate, toDate);
        payload["tpActivePrice"] = Num(tpActivePrice);
        payload["slActivePrice"] = Num(slActivePrice);
        payload["tpPrice"] = tpPrice.Resolve().Text;
        payload["slPrice"] = slPrice.Resolve().Text;
        payload["tpSlip"] = Num(tpSlip);
        payload["slSlip"] = Num(slSlip);
        // The server expects these placeholder fields on an OCO order.
        payload["price"] = OrderType.MP;
        payload["priceSlip"] = 0;
        payload["stopPrice"] = 0;
        payload["activePrice"] = 0;
        payload["trailingAmount"] = 0;
        payload["operator"] = string.Empty;
        payload["code"] = code ?? string.Empty;
        return payload;
    }

    public Task<FCOPlaceResponse> PlaceFcoBullBearAsync(
        string accountNo, string symbol, string side, int quantity, FcoPrice price, decimal priceSlip,
        decimal tpActivePrice, decimal slActivePrice, FcoPrice tpPrice, FcoPrice slPrice, decimal tpSlip, decimal slSlip,
        string fromDate, string toDate, string? code = null, CancellationToken ct = default) =>
        PlaceFcoAsync(BuildFcoBullBear(Config, accountNo, symbol, side, quantity, price, priceSlip, tpActivePrice, slActivePrice, tpPrice, slPrice, tpSlip, slSlip, fromDate, toDate, code), ct);

    internal static Dictionary<string, object?> BuildFcoBullBear(
        Config config, string accountNo, string symbol, string side, int quantity, FcoPrice price, decimal priceSlip,
        decimal tpActivePrice, decimal slActivePrice, FcoPrice tpPrice, FcoPrice slPrice, decimal tpSlip, decimal slSlip,
        string fromDate, string toDate, string? code)
    {
        var payload = FcoBase(config, FCOType.BullBear, accountNo, symbol, side, quantity, fromDate, toDate);
        var main = price.Resolve();
        var tp = tpPrice.Resolve();
        var sl = slPrice.Resolve();
        payload["price"] = main.Text;
        payload["priceSlip"] = main.KeepSlip ? Num(priceSlip) : 0;
        payload["tpActivePrice"] = Num(tpActivePrice);
        payload["slActivePrice"] = Num(slActivePrice);
        payload["tpPrice"] = tp.Text;
        payload["slPrice"] = sl.Text;
        payload["tpSlip"] = tp.KeepSlip ? Num(tpSlip) : 0;
        payload["slSlip"] = sl.KeepSlip ? Num(slSlip) : 0;
        AddCode(payload, code);
        return payload;
    }

    internal static Dictionary<string, object?> BuildFcoCancel(Config config, string fcoId, string? code)
    {
        Validate.RequireNonEmpty(fcoId, "fcoId");
        var payload = new Dictionary<string, object?>
        {
            ["fcoId"] = fcoId,
            ["deviceId"] = DeviceId.Get(),
        };
        if (!string.IsNullOrWhiteSpace(config.UserAgent)) payload["userAgent"] = config.UserAgent;
        AddCode(payload, code);
        return payload;
    }

    public async Task<FCOCancelResponse> CancelFcoAsync(string fcoId, string? code = null, CancellationToken ct = default)
    {
        var (json, headers) = Sign(BuildFcoCancel(Config, fcoId, code));
        var data = await _rest.DeleteAsync(Constants.EpTradingFcoOrder, json, headers, ct).ConfigureAwait(false);
        return FCOCancelResponse.FromJson(data);
    }

    internal static List<KeyValuePair<string, string>> BuildFcoList(
        string accountNo, string? fcoId, string? type, IEnumerable<string>? processStatus, string? symbol, string? side,
        string? fromDate, string? toDate, int? pageIndex, int? pageSize)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        if (fromDate is not null && toDate is not null)
            Validate.RequireDateRange(fromDate, toDate, allowTime: true);
        else
        {
            if (fromDate is not null) Validate.ParseDateArg(fromDate, "from", allowTime: true);
            if (toDate is not null) Validate.ParseDateArg(toDate, "to", allowTime: true);
        }

        var query = new List<KeyValuePair<string, string>> { new("accountNo", accountNo) };
        if (fcoId is not null) query.Add(new("fcoId", fcoId));
        if (type is not null) query.Add(new("type", type));
        // Several statuses go out as a repeated processStatus key.
        if (processStatus is not null)
            foreach (var status in processStatus) query.Add(new("processStatus", status));
        if (symbol is not null) query.Add(new("symbol", symbol));
        if (side is not null) query.Add(new("side", side));
        if (fromDate is not null) query.Add(new("from", fromDate));
        if (toDate is not null) query.Add(new("to", toDate));
        if (pageIndex is not null) query.Add(new("pageIndex", pageIndex.Value.ToString(CultureInfo.InvariantCulture)));
        if (pageSize is not null) query.Add(new("pageSize", pageSize.Value.ToString(CultureInfo.InvariantCulture)));
        return query;
    }

    private async Task<FCOListResponse> GetFcoListAsync(
        string accountNo, string? fcoId = null, IEnumerable<string>? processStatus = null, string? symbol = null,
        string? fromDate = null, string? toDate = null, int? pageIndex = null, int? pageSize = null, CancellationToken ct = default)
    {
        var query = BuildFcoList(accountNo, fcoId, null, processStatus, symbol, null, fromDate, toDate, pageIndex, pageSize);
        var data = await _rest.GetAsync(Constants.EpTradingFcoList, query, ct: ct).ConfigureAwait(false);
        return FCOListResponse.FromJson(data);
    }

    public Task<FCOListResponse> GetFcoByAccountNoAsync(string accountNo, int? pageIndex = null, int? pageSize = null, CancellationToken ct = default) =>
        GetFcoListAsync(accountNo, pageIndex: pageIndex, pageSize: pageSize, ct: ct);

    public Task<FCOListResponse> GetFcoBySymbolAsync(string accountNo, string symbol, int? pageIndex = null, int? pageSize = null, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        return GetFcoListAsync(accountNo, symbol: symbol, pageIndex: pageIndex, pageSize: pageSize, ct: ct);
    }

    public Task<FCOListResponse> GetFcoByStatusAsync(string accountNo, string processStatus, int? pageIndex = null, int? pageSize = null, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(processStatus, "processStatus");
        return GetFcoListAsync(accountNo, processStatus: [processStatus], pageIndex: pageIndex, pageSize: pageSize, ct: ct);
    }

    /// <summary>Several statuses go out as a repeated <c>processStatus</c> query key.</summary>
    public Task<FCOListResponse> GetFcoByStatusAsync(string accountNo, IReadOnlyCollection<string> processStatus, int? pageIndex = null, int? pageSize = null, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(processStatus, "processStatus");
        return GetFcoListAsync(accountNo, processStatus: processStatus, pageIndex: pageIndex, pageSize: pageSize, ct: ct);
    }

    public Task<FCOListResponse> GetFcoByDateAsync(string accountNo, string fromDate, string toDate, int? pageIndex = null, int? pageSize = null, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(fromDate, "from");
        Validate.RequireNonEmpty(toDate, "to");
        return GetFcoListAsync(accountNo, fromDate: fromDate, toDate: toDate, pageIndex: pageIndex, pageSize: pageSize, ct: ct);
    }

    public async Task<FCOInfo?> GetFcoByIdAsync(string accountNo, string fcoId, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(fcoId, "fcoId");
        var response = await GetFcoListAsync(accountNo, fcoId: fcoId, ct: ct).ConfigureAwait(false);
        return response.FCOList.FirstOrDefault();
    }

    public async Task<FCOOrderBookResponse> GetFcoOrderBookAsync(string fcoId, int pageIndex = 1, int pageSize = 10, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(fcoId, "fcoId");
        var query = new List<KeyValuePair<string, string>>
        {
            new("fcoId", fcoId),
            new("pageIndex", pageIndex.ToString(CultureInfo.InvariantCulture)),
            new("pageSize", pageSize.ToString(CultureInfo.InvariantCulture)),
        };
        var data = await _rest.GetAsync(Constants.EpTradingFcoOrderBook, query, ct: ct).ConfigureAwait(false);
        return FCOOrderBookResponse.FromJson(data);
    }

    /// <summary>Status transitions of a conditional order.</summary>
    public async Task<List<FCOStatusHistoryItem>> GetFcoStatusHistoryAsync(string fcoId, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(fcoId, "fcoId");
        // The server binds this query key case-insensitively.
        var query = new List<KeyValuePair<string, string>> { new("fcoid", fcoId) };
        var data = await _rest.GetAsync(Constants.EpTradingFcoStatusHistory, query, ct: ct).ConfigureAwait(false);
        return FCOStatusHistoryItem.FromResponse(data);
    }
}

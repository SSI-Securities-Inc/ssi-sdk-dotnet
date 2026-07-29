using System.Globalization;
using System.Text.Json;
using SsiSdk.Internal;
using SsiSdk.Models;
using SsiSdk.Transport;

namespace SsiSdk.Services;

public sealed class TradingService
{
    private static readonly Logger Log = new("ssi_sdk.services.trading");
    private const string DeviceId = "A1:B2:C3:D4:E5:F6";
    private static readonly string UserAgent = $"SSI .NET SDK/{SdkVersion.Value}";

    private readonly RestClient _rest;
    private readonly string _privateKey;

    internal TradingService(RestClient rest, string privateKey)
    {
        _rest = rest;
        _privateKey = privateKey;
    }

    private (string json, string signature) SerializeAndSign(Dictionary<string, object> payload)
    {
        if (string.IsNullOrEmpty(_privateKey))
            throw new SsiException("Private key is required for trading operations");
        var json = JsonSerializer.Serialize(payload);
        var sig = Crypto.Sign(json, _privateKey);
        Log.Info($"[DEBUG] signData: {json}");
        Log.Info($"[DEBUG] signature: {sig}");
        return (json, sig);
    }

    public async Task<PlaceOrderResponse> PlaceOrderAsync(
        string accountNo, string symbol, string side, int quantity, double price, string orderType,
        CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequireNonEmpty(side, "side");
        Validate.RequireNonEmpty(orderType, "orderType");
        Validate.RequireNonNegative(price, "price");

        var clientRequestId = IdGenerator.GenerateRequestId();
        var payload = new Dictionary<string, object>
        {
            ["accountNo"] = accountNo,
            ["symbol"] = symbol,
            ["side"] = side,
            ["quantity"] = quantity,
            ["price"] = price.ToString(CultureInfo.InvariantCulture),
            ["orderType"] = orderType,
            ["clientRequestId"] = clientRequestId,
            ["deviceId"] = DeviceId,
            ["userAgent"] = UserAgent,
        };

        var (json, sig) = SerializeAndSign(payload);
        var headers = new Dictionary<string, string> { [Constants.HeaderSignature] = sig };
        var data = await _rest.PostAsync(Constants.EpTradingOrder, json, headers, ct);
        var d = Converter.GetProp(data, "data");
        return d is not null ? PlaceOrderResponse.FromJson(d.Value) : PlaceOrderResponse.FromJson(data);
    }

    public Task<PlaceOrderResponse> PlaceLimitOrderAsync(string accountNo, string symbol, string side, int quantity, double price, CancellationToken ct = default)
    {
        Validate.RequirePositive(price, "price");
        return PlaceOrderAsync(accountNo, symbol, side, quantity, price, OrderType.LO, ct);
    }

    public Task<PlaceOrderResponse> PlaceMarketOrderAsync(string accountNo, string symbol, string side, int quantity, CancellationToken ct = default) =>
        PlaceOrderAsync(accountNo, symbol, side, quantity, 0, OrderType.MTL, ct);

    public Task<PlaceOrderResponse> PlaceAtoOrderAsync(string accountNo, string symbol, string side, int quantity, CancellationToken ct = default) =>
        PlaceOrderAsync(accountNo, symbol, side, quantity, 0, OrderType.ATO, ct);

    public Task<PlaceOrderResponse> PlaceAtcOrderAsync(string accountNo, string symbol, string side, int quantity, CancellationToken ct = default) =>
        PlaceOrderAsync(accountNo, symbol, side, quantity, 0, OrderType.ATC, ct);

    private async Task<ModifyOrderResponse> ModifyOrderAsync(
        string accountNo, string? orderId, string? clientRequestId,
        double? price, int? quantity, CancellationToken ct)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        if (price is not null)
        {
            Validate.RequireNonNegative(price.Value, "price");
            Validate.RequireEmpty(quantity, "quantity");
        }
        if (quantity is not null)
        {
            Validate.RequirePositive(quantity.Value, "quantity");
            Validate.RequireEmpty(price, "price");
        }

        var payload = new Dictionary<string, object>
        {
            ["accountNo"] = accountNo,
            ["clientModifyId"] = IdGenerator.GenerateRequestId(),
            ["deviceId"] = DeviceId,
            ["userAgent"] = UserAgent,
        };
        if (!string.IsNullOrEmpty(orderId)) payload["orderId"] = orderId;
        if (!string.IsNullOrEmpty(clientRequestId)) payload["clientRequestId"] = clientRequestId;
        if (price is not null) payload["price"] = price.Value.ToString(CultureInfo.InvariantCulture);
        if (quantity is not null) payload["quantity"] = quantity.Value;

        var (json, sig) = SerializeAndSign(payload);
        var headers = new Dictionary<string, string> { [Constants.HeaderSignature] = sig };
        var data = await _rest.PutAsync(Constants.EpTradingOrder, json, headers, ct);
        var d = Converter.GetProp(data, "data");
        return d is not null ? ModifyOrderResponse.FromJson(d.Value) : ModifyOrderResponse.FromJson(data);
    }

    public Task<ModifyOrderResponse> ModifyOrderPriceAsync(string accountNo, string clientRequestId, double price, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(clientRequestId, "clientRequestId");
        Validate.RequireNonNegative(price, "price");
        return ModifyOrderAsync(accountNo, null, clientRequestId, price, null, ct);
    }

    public Task<ModifyOrderResponse> ModifyOrderPriceByOrderIdAsync(string accountNo, string orderId, double price, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(orderId, "orderId");
        Validate.RequireNonNegative(price, "price");
        return ModifyOrderAsync(accountNo, orderId, null, price, null, ct);
    }

    public Task<ModifyOrderResponse> ModifyOrderQuantityAsync(string accountNo, string clientRequestId, int quantity, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(clientRequestId, "clientRequestId");
        Validate.RequirePositive(quantity, "quantity");
        return ModifyOrderAsync(accountNo, null, clientRequestId, null, quantity, ct);
    }

    public Task<ModifyOrderResponse> ModifyOrderQuantityByOrderIdAsync(string accountNo, string orderId, int quantity, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(orderId, "orderId");
        Validate.RequirePositive(quantity, "quantity");
        return ModifyOrderAsync(accountNo, orderId, null, null, quantity, ct);
    }

    private async Task<CancelOrderResponse> CancelOrderInternalAsync(
        string accountNo, string? orderId, string? clientRequestId, CancellationToken ct)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");

        var payload = new Dictionary<string, object>
        {
            ["accountNo"] = accountNo,
            ["clientCancelId"] = IdGenerator.GenerateRequestId(),
            ["deviceId"] = DeviceId,
            ["userAgent"] = UserAgent,
        };
        if (!string.IsNullOrEmpty(orderId)) payload["orderId"] = orderId;
        if (!string.IsNullOrEmpty(clientRequestId)) payload["clientRequestId"] = clientRequestId;

        var (json, sig) = SerializeAndSign(payload);
        var headers = new Dictionary<string, string> { [Constants.HeaderSignature] = sig };
        var data = await _rest.DeleteAsync(Constants.EpTradingOrder, json, headers, ct);
        var d = Converter.GetProp(data, "data");
        return d is not null ? CancelOrderResponse.FromJson(d.Value) : CancelOrderResponse.FromJson(data);
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

    public async Task<MaxBuySellResponse> GetMaxBuySellAsync(string accountNo, string symbol, double price, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequirePositive(price, "price");
        var p = new Dictionary<string, string>
        {
            ["accountNo"] = accountNo,
            ["symbol"] = symbol.ToUpperInvariant(),
            ["price"] = price.ToString(CultureInfo.InvariantCulture),
        };
        var data = await _rest.GetAsync(Constants.EpTradingMaxBuySell, p, ct: ct);
        return MaxBuySellResponse.FromJson(data, symbol);
    }

    public async Task<MaxBuySellResponse> GetMaxBuySellAtMarketPriceAsync(string accountNo, string symbol, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");
        var p = new Dictionary<string, string>
        {
            ["accountNo"] = accountNo,
            ["symbol"] = symbol.ToUpperInvariant(),
        };
        var data = await _rest.GetAsync(Constants.EpTradingMaxBuySell, p, ct: ct);
        return MaxBuySellResponse.FromJson(data, symbol);
    }

    // ---------------------------------------------------------------------------
    // Flexible Conditional Orders (FCO)
    // ---------------------------------------------------------------------------

    private async Task<FCOPlaceResponse> PlaceFcoOrderInternalAsync(Dictionary<string, object> payload, CancellationToken ct)
    {
        payload["deviceId"] = DeviceId;
        payload["userAgent"] = UserAgent;

        var (json, sig) = SerializeAndSign(payload);
        var headers = new Dictionary<string, string> { [Constants.HeaderSignature] = sig };
        var data = await _rest.PostAsync(Constants.EpTradingFcoOrder, json, headers, ct);
        var res = JsonSerializer.Deserialize<FCOPlaceResponse>(data.GetRawText());
        return res ?? new FCOPlaceResponse();
    }

    public Task<FCOPlaceResponse> PlaceFcoGtdAsync(
        string accountNo, string symbol, string side, int quantity, object price, double priceSlip,
        string fromDate, string toDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequireNonEmpty(side, "side");

        var isStringPrice = price is string;
        var priceStr = isStringPrice ? (string)price : Convert.ToDouble(price, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        var slip = isStringPrice ? 0 : priceSlip;


        var payload = new Dictionary<string, object>
        {
            ["accountNo"] = accountNo,
            ["type"] = FCOType.GTD,
            ["symbol"] = symbol,
            ["side"] = side,
            ["price"] = priceStr,
            ["priceSlip"] = slip,
            ["quantity"] = quantity,
            ["from"] = fromDate,
            ["to"] = toDate,
        };
        return PlaceFcoOrderInternalAsync(payload, ct);
    }

    public Task<FCOPlaceResponse> PlaceFcoStopAsync(
        string accountNo, string symbol, string side, int quantity, double stopPrice, string operatorType,
        string fromDate, string toDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");

        var payload = new Dictionary<string, object>
        {
            ["accountNo"] = accountNo,
            ["type"] = FCOType.Stop,
            ["symbol"] = symbol,
            ["side"] = side,
            ["price"] = OrderType.MTL,
            ["priceSlip"] = 0,
            ["quantity"] = quantity,
            ["stopPrice"] = stopPrice,
            ["operator"] = operatorType,
            ["from"] = fromDate,
            ["to"] = toDate,
        };
        return PlaceFcoOrderInternalAsync(payload, ct);
    }

    public Task<FCOPlaceResponse> PlaceFcoStopLimitAsync(
        string accountNo, string symbol, string side, int quantity, object price, double priceSlip, double stopPrice, string operatorType,
        string fromDate, string toDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");

        var priceStr = price.ToString()!;
        var payload = new Dictionary<string, object>
        {
            ["accountNo"] = accountNo,
            ["type"] = FCOType.StopLimit,
            ["symbol"] = symbol,
            ["side"] = side,
            ["price"] = priceStr,
            ["priceSlip"] = priceSlip,
            ["quantity"] = quantity,
            ["stopPrice"] = stopPrice,
            ["operator"] = operatorType,
            ["from"] = fromDate,
            ["to"] = toDate,
        };
        return PlaceFcoOrderInternalAsync(payload, ct);
    }

    public Task<FCOPlaceResponse> PlaceFcoTrailingStopAsync(
        string accountNo, string symbol, string side, int quantity, double activePrice, double trailingAmount,
        string fromDate, string toDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");

        var payload = new Dictionary<string, object>
        {
            ["accountNo"] = accountNo,
            ["type"] = FCOType.TrailingStop,
            ["symbol"] = symbol,
            ["side"] = side,
            ["quantity"] = quantity,
            ["activePrice"] = activePrice,
            ["trailingAmount"] = trailingAmount,
            ["price"] = OrderType.MTL,
            ["priceSlip"] = 0,
            ["from"] = fromDate,
            ["to"] = toDate,
        };
        return PlaceFcoOrderInternalAsync(payload, ct);
    }

    public Task<FCOPlaceResponse> PlaceFcoTrailingStopLimitAsync(
        string accountNo, string symbol, string side, int quantity, double activePrice, double trailingAmount, double priceSlip,
        string fromDate, string toDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");

        var payload = new Dictionary<string, object>
        {
            ["accountNo"] = accountNo,
            ["type"] = FCOType.TrailingStopLimit,
            ["symbol"] = symbol,
            ["side"] = side,
            ["quantity"] = quantity,
            ["activePrice"] = activePrice,
            ["trailingAmount"] = trailingAmount,
            ["priceSlip"] = priceSlip,
            ["from"] = fromDate,
            ["to"] = toDate,
        };
        return PlaceFcoOrderInternalAsync(payload, ct);
    }

    public Task<FCOPlaceResponse> PlaceFcoOcoAsync(
        string accountNo, string symbol, string side, int quantity, double tpActivePrice, double slActivePrice,
        object tpPrice, object slPrice, double tpSlip, double slSlip,
        string fromDate, string toDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");

        var payload = new Dictionary<string, object>
        {
            ["accountNo"] = accountNo,
            ["type"] = FCOType.OCO,
            ["symbol"] = symbol,
            ["side"] = side,
            ["quantity"] = quantity,
            ["tpActivePrice"] = tpActivePrice,
            ["slActivePrice"] = slActivePrice,
            ["tpPrice"] = tpPrice.ToString()!,
            ["slPrice"] = slPrice.ToString()!,
            ["tpSlip"] = tpSlip,
            ["slSlip"] = slSlip,
            ["from"] = fromDate,
            ["to"] = toDate,
            ["price"] = "MP",
            ["priceSlip"] = 0,
            ["stopPrice"] = 0,
            ["activePrice"] = 0,
            ["trailingAmount"] = 0,
            ["operator"] = "",
            ["code"] = "",
        };
        return PlaceFcoOrderInternalAsync(payload, ct);
    }

    public Task<FCOPlaceResponse> PlaceFcoBullBearAsync(
        string accountNo, string symbol, string side, int quantity, object price, double priceSlip,
        double tpActivePrice, double slActivePrice, object tpPrice, object slPrice, double tpSlip, double slSlip,
        string fromDate, string toDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");

        var payload = new Dictionary<string, object>
        {
            ["accountNo"] = accountNo,
            ["type"] = FCOType.BullBear,
            ["symbol"] = symbol,
            ["side"] = side,
            ["quantity"] = quantity,
            ["price"] = price.ToString()!,
            ["priceSlip"] = priceSlip,
            ["tpActivePrice"] = tpActivePrice,
            ["slActivePrice"] = slActivePrice,
            ["tpPrice"] = tpPrice.ToString()!,
            ["slPrice"] = slPrice.ToString()!,
            ["tpSlip"] = tpSlip,
            ["slSlip"] = slSlip,
            ["from"] = fromDate,
            ["to"] = toDate,
        };
        return PlaceFcoOrderInternalAsync(payload, ct);
    }

    public async Task<FCOCancelResponse> CancelFcoAsync(string fcoId, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(fcoId, "fcoId");
        var payload = new Dictionary<string, object>
        {
            ["fcoId"] = fcoId,
            ["deviceId"] = DeviceId,
            ["userAgent"] = UserAgent,
        };
        var (json, sig) = SerializeAndSign(payload);
        var headers = new Dictionary<string, string> { [Constants.HeaderSignature] = sig };
        var data = await _rest.DeleteAsync(Constants.EpTradingFcoOrder, json, headers, ct);
        var res = JsonSerializer.Deserialize<FCOCancelResponse>(data.GetRawText());
        return res ?? new FCOCancelResponse();
    }

    public async Task<FCOListResponse> GetFcoByAccountNoAsync(string accountNo, int pageIndex = 1, int pageSize = 10, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var p = new Dictionary<string, string>
        {
            ["accountNo"] = accountNo,
            ["pageIndex"] = pageIndex.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        var data = await _rest.GetAsync(Constants.EpTradingFcoList, p, ct: ct);
        var res = JsonSerializer.Deserialize<FCOListResponse>(data.GetRawText());
        return res ?? new FCOListResponse();
    }

    public async Task<FCOListResponse> GetFcoBySymbolAsync(string accountNo, string symbol, int pageIndex = 1, int pageSize = 10, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(symbol, "symbol");
        var p = new Dictionary<string, string>
        {
            ["accountNo"] = accountNo,
            ["symbol"] = symbol,
            ["pageIndex"] = pageIndex.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        var data = await _rest.GetAsync(Constants.EpTradingFcoList, p, ct: ct);
        var res = JsonSerializer.Deserialize<FCOListResponse>(data.GetRawText());
        return res ?? new FCOListResponse();
    }

    public async Task<FCOListResponse> GetFcoByStatusAsync(string accountNo, string processStatus, int pageIndex = 1, int pageSize = 10, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(processStatus, "processStatus");
        var p = new Dictionary<string, string>
        {
            ["accountNo"] = accountNo,
            ["processStatus"] = processStatus,
            ["pageIndex"] = pageIndex.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        var data = await _rest.GetAsync(Constants.EpTradingFcoList, p, ct: ct);
        var res = JsonSerializer.Deserialize<FCOListResponse>(data.GetRawText());
        return res ?? new FCOListResponse();
    }

    public async Task<FCOListResponse> GetFcoByDateAsync(string accountNo, string fromDate, string toDate, int pageIndex = 1, int pageSize = 10, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var p = new Dictionary<string, string>
        {
            ["accountNo"] = accountNo,
            ["fromDate"] = fromDate,
            ["toDate"] = toDate,
            ["pageIndex"] = pageIndex.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        var data = await _rest.GetAsync(Constants.EpTradingFcoList, p, ct: ct);
        var res = JsonSerializer.Deserialize<FCOListResponse>(data.GetRawText());
        return res ?? new FCOListResponse();
    }

    public async Task<FCOInfo?> GetFcoByIdAsync(string accountNo, string fcoId, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(fcoId, "fcoId");
        var p = new Dictionary<string, string>
        {
            ["accountNo"] = accountNo,
            ["fcoId"] = fcoId,
        };
        var data = await _rest.GetAsync(Constants.EpTradingFcoList, p, ct: ct);
        var res = JsonSerializer.Deserialize<FCOListResponse>(data.GetRawText());
        return res?.FCOList.FirstOrDefault();
    }

    public async Task<FCOOrderBookResponse> GetFcoOrderBookAsync(string fcoId, int pageIndex = 1, int pageSize = 10, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(fcoId, "fcoId");
        var p = new Dictionary<string, string>
        {
            ["fcoId"] = fcoId,
            ["pageIndex"] = pageIndex.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        var data = await _rest.GetAsync(Constants.EpTradingFcoOrderBook, p, ct: ct);
        var res = JsonSerializer.Deserialize<FCOOrderBookResponse>(data.GetRawText());
        return res ?? new FCOOrderBookResponse();
    }
}


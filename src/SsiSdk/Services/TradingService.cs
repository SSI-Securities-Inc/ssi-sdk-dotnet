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
}

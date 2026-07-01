using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Models;

public sealed class PlaceOrderResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string ClientRequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    internal static PlaceOrderResponse FromJson(JsonElement el) => new()
    {
        OrderId = Converter.ToStr(Converter.GetProp(el, "orderId")),
        ClientRequestId = Converter.ToStr(Converter.GetProp(el, "clientRequestId")),
        Status = Converter.ToStr(Converter.GetProp(el, "orderStatus")),
    };
}

public sealed class ModifyOrderResponse
{
    public string ClientModifyId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string ClientRequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    internal static ModifyOrderResponse FromJson(JsonElement el) => new()
    {
        ClientModifyId = Converter.ToStr(Converter.GetProp(el, "clientModifyId")),
        OrderId = Converter.ToStr(Converter.GetProp(el, "orderId")),
        ClientRequestId = Converter.ToStr(Converter.GetProp(el, "clientRequestId")),
        Status = Converter.ToStr(Converter.GetProp(el, "orderStatus")),
    };
}

public sealed class CancelOrderResponse
{
    public string ClientCancelId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string ClientRequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    internal static CancelOrderResponse FromJson(JsonElement el) => new()
    {
        ClientCancelId = Converter.ToStr(Converter.GetProp(el, "clientCancelId")),
        OrderId = Converter.ToStr(Converter.GetProp(el, "orderId")),
        ClientRequestId = Converter.ToStr(Converter.GetProp(el, "clientRequestId")),
        Status = Converter.ToStr(Converter.GetProp(el, "orderStatus")),
    };
}

public sealed class MaxBuySellResponse
{
    public string AccountNo { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int MaxBuyQuantity { get; set; }
    public int MaxSellQuantity { get; set; }
    public string MarginRatio { get; set; } = string.Empty;
    public string PurchasePower { get; set; } = string.Empty;

    internal static MaxBuySellResponse FromJson(JsonElement el, string symbol) => new()
    {
        AccountNo = Converter.ToStr(Converter.GetProp(el, "accountNo")),
        Symbol = symbol.ToUpperInvariant(),
        MaxBuyQuantity = Converter.ToInt(Converter.GetProp(el, "maxBuyQty")),
        MaxSellQuantity = Converter.ToInt(Converter.GetProp(el, "maxSellQty")),
        MarginRatio = Converter.ToStr(Converter.GetProp(el, "marginRatio")),
        PurchasePower = Converter.ToStr(Converter.GetProp(el, "purchasePower")),
    };
}

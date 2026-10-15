using System.Globalization;
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
    public string? MarginRatio { get; set; }

    /// <summary>Purchasing power in VND; null when the server did not return it.</summary>
    public long? PurchasingPower { get; set; }

    [Obsolete("Use PurchasingPower")]
    public string PurchasePower => PurchasingPower is null ? string.Empty : PurchasingPower.Value.ToString(CultureInfo.InvariantCulture);

    internal static MaxBuySellResponse FromJson(JsonElement el, string symbol) => new()
    {
        AccountNo = Converter.ToStr(Converter.GetProp(el, "accountNo")),
        Symbol = symbol.ToUpperInvariant(),
        MaxBuyQuantity = Converter.ToInt(Converter.GetProp(el, "maxBuyQty")),
        MaxSellQuantity = Converter.ToInt(Converter.GetProp(el, "maxSellQty")),
        MarginRatio = Converter.ToOptStr(Converter.GetProp(el, "marginRatio")),
        PurchasingPower = Converter.ToOptInt64(Converter.Pick(el, "purchasingPower", "purchasePower")),
    };
}

/// <summary>One order of <c>PlaceBatchOrdersAsync</c> (the same arguments as <c>PlaceOrderAsync</c>).</summary>
public sealed record BatchPlaceOrderItem(
    string AccountNo,
    string Symbol,
    string Side,
    int Quantity,
    decimal? Price,
    string OrderType,
    string? ClientRequestId = null);

/// <summary>One order of <c>CancelBatchOrdersAsync</c>: <c>AccountNo</c> plus exactly one of the two ids.</summary>
public sealed record BatchCancelOrderItem(string AccountNo, string? OrderId = null, string? ClientRequestId = null);

/// <summary>Outcome of one order of a batch.</summary>
public sealed class BatchOrderResult
{
    public string ClientRequestId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ClientCancelId { get; set; } = string.Empty;

    internal static BatchOrderResult FromJson(JsonElement d) => new()
    {
        ClientRequestId = Converter.ToStr(Converter.GetProp(d, "clientRequestId")),
        OrderId = Converter.ToStr(Converter.GetProp(d, "orderId")),
        Status = Converter.ToStr(Converter.GetProp(d, "orderStatus")),
        Success = Converter.ToBool(Converter.GetProp(d, "success")),
        ErrorCode = Converter.ToStr(Converter.GetProp(d, "errorCode")),
        ErrorMessage = Converter.ToStr(Converter.GetProp(d, "errorMessage")),
        ClientCancelId = Converter.ToStr(Converter.GetProp(d, "clientCancelId")),
    };
}

/// <summary>Response of a batch request: <c>{"batchRequestId", "results": [...]}</c>.</summary>
public sealed class BatchOrderResponse
{
    public string BatchRequestId { get; set; } = string.Empty;
    public List<BatchOrderResult> Results { get; set; } = [];

    internal static BatchOrderResponse FromJson(JsonElement el)
    {
        var d = Converter.Payload(el);
        return new BatchOrderResponse
        {
            BatchRequestId = Converter.ToStr(Converter.GetProp(d, "batchRequestId")),
            Results = Converter.GetArray(Converter.GetProp(d, "results"))
                .Where(r => r.ValueKind == JsonValueKind.Object)
                .Select(BatchOrderResult.FromJson)
                .ToList(),
        };
    }
}

/// <summary>
/// A price field of a conditional order: a number (<c>100.5m</c>, <c>25000</c>) or an order-type word
/// (<c>"MP"</c>, <c>OrderType.MTL</c>). Strings are sent as given.
/// </summary>
public readonly record struct FcoPrice
{
    private static readonly HashSet<string> MarketWords = new(StringComparer.OrdinalIgnoreCase)
    {
        OrderType.ATO, OrderType.ATC, OrderType.MTL, OrderType.MP, OrderType.MOK, OrderType.MAK,
    };

    public decimal? Number { get; }
    public string? Word { get; }

    public FcoPrice(decimal number) { Number = number; Word = null; }
    public FcoPrice(string word) { Number = null; Word = word; }

    public static implicit operator FcoPrice(decimal number) => new(number);
    public static implicit operator FcoPrice(int number) => new((decimal)number);
    public static implicit operator FcoPrice(long number) => new((decimal)number);
    public static implicit operator FcoPrice(string word) => new(word);

    /// <summary>The wire text and whether the slip applies (a market word means no slip).</summary>
    internal (string Text, bool KeepSlip) Resolve()
    {
        if (Number is { } n) return (Converter.FormatPrice(n), true);
        var text = Word ?? string.Empty;
        return (text, !MarketWords.Contains(text));
    }
}

using System.Text.Json.Serialization;

namespace SsiSdk.Models;

public record FCOParams
{
    [JsonPropertyName("stopPrice")] public double StopPrice { get; init; }
    [JsonPropertyName("side")] public string Side { get; init; } = string.Empty;
    [JsonPropertyName("activePrice")] public double ActivePrice { get; init; }
    [JsonPropertyName("trailingAmount")] public double TrailingAmount { get; init; }
    [JsonPropertyName("tpActivePrice")] public double TPActivePrice { get; init; }
    [JsonPropertyName("slActivePrice")] public double SLActivePrice { get; init; }
    [JsonPropertyName("tpPrice")] public string TPPrice { get; init; } = string.Empty;
    [JsonPropertyName("slPrice")] public string SLPrice { get; init; } = string.Empty;
    [JsonPropertyName("tpSlip")] public double TPSlip { get; init; }
    [JsonPropertyName("slSlip")] public double SLSlip { get; init; }
    [JsonPropertyName("operator")] public string Operator { get; init; } = string.Empty;
}

public record FCOInfo
{
    [JsonPropertyName("fcoId")] public string FCOID { get; init; } = string.Empty;
    [JsonPropertyName("username")] public string ClientID { get; init; } = string.Empty;
    [JsonPropertyName("accountNo")] public string AccountNo { get; init; } = string.Empty;
    [JsonPropertyName("quantity")] public int Quantity { get; init; }
    [JsonPropertyName("price")] public string Price { get; init; } = string.Empty;
    [JsonPropertyName("priceSlip")] public double PriceSlip { get; init; }
    [JsonPropertyName("symbol")] public string Symbol { get; init; } = string.Empty;
    [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;
    [JsonPropertyName("fromDate")] public string FromDate { get; init; } = string.Empty;
    [JsonPropertyName("toDate")] public string ToDate { get; init; } = string.Empty;
    [JsonPropertyName("matchedQuantity")] public int MatchedQuantity { get; init; }
    [JsonPropertyName("isPlaceOrder")] public bool IsPlaceOrder { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("detail")] public string Detail { get; init; } = string.Empty;
    [JsonPropertyName("params")] public FCOParams? Params { get; init; }
}

public record FCOListResponse
{
    [JsonPropertyName("pageIndex")] public int PageIndex { get; init; } = 1;
    [JsonPropertyName("pageSize")] public int PageSize { get; init; } = 10;
    [JsonPropertyName("itemsCount")] public int ItemsCount { get; init; }
    [JsonPropertyName("pagesCount")] public int PagesCount { get; init; }
    [JsonPropertyName("data")] public List<FCOInfo> FCOList { get; init; } = new();
}

public record FCOOrder
{
    [JsonPropertyName("fcoId")] public string FCOID { get; init; } = string.Empty;
    [JsonPropertyName("accountNo")] public string AccountNo { get; init; } = string.Empty;
    [JsonPropertyName("quantity")] public double Quantity { get; init; }
    [JsonPropertyName("price")] public string Price { get; init; } = string.Empty;
    [JsonPropertyName("symbol")] public string Symbol { get; init; } = string.Empty;
    [JsonPropertyName("side")] public string Side { get; init; } = string.Empty;
    [JsonPropertyName("orderType")] public string OrderType { get; init; } = string.Empty;
    [JsonPropertyName("isMainOrder")] public bool IsMainOrder { get; init; }
    [JsonPropertyName("isAttachedOrder")] public bool IsAttachedOrder { get; init; }
    [JsonPropertyName("createdTime")] public string CreatedTime { get; init; } = string.Empty;
    [JsonPropertyName("updatedTime")] public string UpdatedTime { get; init; } = string.Empty;
    [JsonPropertyName("uniqueId")] public string UniqueID { get; init; } = string.Empty;
    [JsonPropertyName("orderId")] public string OrderID { get; init; } = string.Empty;
    [JsonPropertyName("matchedQuantity")] public double MatchedQuantity { get; init; }
    [JsonPropertyName("osQuantity")] public double OSQuantity { get; init; }
    [JsonPropertyName("avgPrice")] public double AvgPrice { get; init; }
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("detail")] public string Detail { get; init; } = string.Empty;
}

public record FCOOrderBookResponse
{
    [JsonPropertyName("pageIndex")] public int PageIndex { get; init; } = 1;
    [JsonPropertyName("pageSize")] public int PageSize { get; init; } = 10;
    [JsonPropertyName("itemsCount")] public int ItemsCount { get; init; }
    [JsonPropertyName("pagesCount")] public int PagesCount { get; init; }
    [JsonPropertyName("data")] public List<FCOOrder> OrderBook { get; init; } = new();
}

public record FCOPlaceResponse
{
    [JsonPropertyName("fcoId")] public string FCOID { get; init; } = string.Empty;
}

public record FCOCancelResponse
{
    [JsonPropertyName("fcoId")] public string FCOID { get; init; } = string.Empty;
}

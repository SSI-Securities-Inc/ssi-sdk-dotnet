using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Models;

/// <summary>Fields every FCO payload carries (REST list / order book and stream events alike).</summary>
internal readonly record struct FcoCommon(
    string FcoId, string AccountNo, string Symbol, int Quantity, int MatchedQuantity, bool IsPlaceOrder, string? Type)
{
    internal static FcoCommon Parse(JsonElement data) => new(
        FcoId: Converter.ToStr(Converter.Pick(data, "fcoId", "FcoId")),
        AccountNo: Converter.ToStr(Converter.Pick(data, "accountNo", "AccountNo")),
        Symbol: Converter.ToStr(Converter.Pick(data, "symbol", "Symbol")),
        Quantity: Converter.ToInt(Converter.Pick(data, "quantity", "Quantity")),
        MatchedQuantity: Converter.ToInt(Converter.Pick(data, "matchedQuantity", "matchedQuality")),
        IsPlaceOrder: Converter.ToBool(Converter.Pick(data, "isPlaceOrder", "IsPlaceOrder")),
        Type: Converter.ToOptStr(Converter.GetProp(data, "type")));
}

/// <summary>The server encodes the trigger operator as a number 0..4 in responses; requests use the name.</summary>
internal static class FcoOperators
{
    private static readonly string[] ByNumber =
        [FCOOperator.GreaterOrEqual, FCOOperator.Equal, FCOOperator.Greater, FCOOperator.Lesser, FCOOperator.LesserOrEqual];

    /// <summary>All operator names, in the server's numeric order.</summary>
    internal static IReadOnlyList<string> All => ByNumber;

    internal static string? Parse(JsonElement? raw)
    {
        if (raw is null) return null;
        var text = Converter.ToStr(raw).Trim();
        if (text.Length == 0) return null;
        if (raw.Value.ValueKind != JsonValueKind.True && raw.Value.ValueKind != JsonValueKind.False
            && int.TryParse(text, out var n) && n >= 0 && n < ByNumber.Length)
            return ByNumber[n];
        var lower = text.ToLowerInvariant();
        return ByNumber.Contains(lower) ? lower : text;
    }

    /// <summary>Accepts an operator name (any case); anything else is a ValidationException.</summary>
    internal static string Require(string? value)
    {
        var lower = value?.Trim().ToLowerInvariant();
        if (lower is not null && ByNumber.Contains(lower)) return lower;
        throw new ValidationException($"operator must be one of {string.Join(", ", ByNumber)}, got '{value}'");
    }
}

/// <summary>Detailed trigger parameters of an FCO order. Unset values are null.</summary>
public record FCOParams
{
    public decimal? StopPrice { get; init; }
    public string? Side { get; init; }
    public decimal? ActivePrice { get; init; }
    public decimal? TrailingAmount { get; init; }
    public decimal? TPActivePrice { get; init; }
    public decimal? SLActivePrice { get; init; }
    public string? TPPrice { get; init; }
    public string? SLPrice { get; init; }
    public decimal? TPSlip { get; init; }
    public decimal? SLSlip { get; init; }
    public string? Operator { get; init; }

    /// <summary>Wire-name dictionary; unset (null) and empty-string values are omitted, a legitimate 0 is kept.</summary>
    public Dictionary<string, object> ToDict()
    {
        var map = new Dictionary<string, object>();
        void Add(string key, object? value)
        {
            if (value is null || value is string { Length: 0 }) return;
            map[key] = value;
        }
        Add("stopPrice", StopPrice);
        Add("side", Side);
        Add("activePrice", ActivePrice);
        Add("trailingAmount", TrailingAmount);
        Add("tpActivePrice", TPActivePrice);
        Add("slActivePrice", SLActivePrice);
        Add("tpPrice", TPPrice);
        Add("slPrice", SLPrice);
        Add("tpSlip", TPSlip);
        Add("slSlip", SLSlip);
        Add("operator", Operator);
        return map;
    }

    internal static FCOParams? FromJson(JsonElement? el)
    {
        if (el is null || el.Value.ValueKind != JsonValueKind.Object || !el.Value.EnumerateObject().Any()) return null;
        var d = el.Value;
        return new FCOParams
        {
            StopPrice = Converter.ToDecimalOrNull(Converter.GetProp(d, "stopPrice")),
            Side = Converter.ToOptStr(Converter.GetProp(d, "side")),
            ActivePrice = Converter.ToDecimalOrNull(Converter.GetProp(d, "activePrice")),
            TrailingAmount = Converter.ToDecimalOrNull(Converter.GetProp(d, "trailingAmount")),
            TPActivePrice = Converter.ToDecimalOrNull(Converter.GetProp(d, "tpActivePrice")),
            SLActivePrice = Converter.ToDecimalOrNull(Converter.GetProp(d, "slActivePrice")),
            TPPrice = Converter.ToOptStr(Converter.GetProp(d, "tpPrice")),
            SLPrice = Converter.ToOptStr(Converter.GetProp(d, "slPrice")),
            TPSlip = Converter.ToDecimalOrNull(Converter.GetProp(d, "tpSlip")),
            SLSlip = Converter.ToDecimalOrNull(Converter.GetProp(d, "slSlip")),
            Operator = FcoOperators.Parse(Converter.Pick(d, "operator", "price_operator")),
        };
    }
}

/// <summary>A conditional order item in the FCO list.</summary>
public record FCOInfo
{
    public string FCOID { get; init; } = string.Empty;
    public string ClientID { get; init; } = string.Empty;
    public string AccountNo { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public string Price { get; init; } = string.Empty;
    public decimal? PriceSlip { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string? Type { get; init; }
    public string FromDate { get; init; } = string.Empty;
    public string ToDate { get; init; } = string.Empty;
    public int MatchedQuantity { get; init; }
    public bool IsPlaceOrder { get; init; }
    public string? Status { get; init; }
    public string Detail { get; init; } = string.Empty;
    public FCOParams? Params { get; init; }
    public string? OrderType { get; init; }

    internal static FCOInfo FromJson(JsonElement data)
    {
        var common = FcoCommon.Parse(data);
        return new FCOInfo
        {
            FCOID = common.FcoId,
            ClientID = Converter.ToStr(Converter.Pick(data, "username", "Username")),
            AccountNo = common.AccountNo,
            Quantity = common.Quantity,
            Price = Converter.ToStr(Converter.GetProp(data, "price")),
            PriceSlip = Converter.ToDecimalOrNull(Converter.GetProp(data, "priceSlip")),
            Symbol = common.Symbol,
            Type = common.Type,
            FromDate = Converter.ToStr(Converter.GetProp(data, "from")),
            ToDate = Converter.ToStr(Converter.GetProp(data, "to")),
            MatchedQuantity = common.MatchedQuantity,
            IsPlaceOrder = common.IsPlaceOrder,
            Status = Converter.ToOptStr(Converter.GetProp(data, "status")),
            Detail = Converter.ToStr(Converter.GetProp(data, "detail")),
            Params = FCOParams.FromJson(Converter.Pick(data, "params", "fco_params", "fcoParams")),
            OrderType = Converter.ToOptStr(Converter.GetProp(data, "orderType")),
        };
    }
}

public record FCOListResponse
{
    public int PageIndex { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int ItemsCount { get; init; }
    public int PagesCount { get; init; }
    public List<FCOInfo> FCOList { get; init; } = new();

    internal static FCOListResponse FromJson(JsonElement data)
    {
        var root = Converter.IsNoContent(data) ? default : data;
        var rows = root.ValueKind == JsonValueKind.Array ? root : Converter.Pick(root, "data", "fcoList", "fco_list");
        return new FCOListResponse
        {
            PageIndex = PageNumber(root, "pageIndex", 1),
            PageSize = PageNumber(root, "pageSize", 10),
            ItemsCount = PageNumber(root, "itemsCount", 0),
            PagesCount = Converter.ToInt(Converter.Pick(root, "pagesCount", "pageCount")),
            FCOList = Converter.GetArray(rows).Where(r => r.ValueKind == JsonValueKind.Object).Select(FCOInfo.FromJson).ToList(),
        };
    }

    internal static int PageNumber(JsonElement root, string name, int fallback) =>
        Converter.ToOptInt(Converter.GetProp(root, name)) ?? fallback;
}

/// <summary>An entry in the FCO order book / execution log.</summary>
public record FCOOrder
{
    public string FCOID { get; init; } = string.Empty;
    public string AccountNo { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public string Price { get; init; } = string.Empty;
    public string Symbol { get; init; } = string.Empty;
    public string? Side { get; init; }
    public string? OrderType { get; init; }
    public bool IsMainOrder { get; init; }
    public bool IsAttachedOrder { get; init; }
    public string CreatedTime { get; init; } = string.Empty;
    public string UpdatedTime { get; init; } = string.Empty;
    public string UniqueID { get; init; } = string.Empty;
    public string OrderID { get; init; } = string.Empty;
    public int MatchedQuantity { get; init; }
    public int OSQuantity { get; init; }
    public decimal AvgPrice { get; init; }
    public string? Status { get; init; }
    public string Detail { get; init; } = string.Empty;
    public string? Type { get; init; }

    internal static FCOOrder FromJson(JsonElement data)
    {
        var common = FcoCommon.Parse(data);
        return new FCOOrder
        {
            FCOID = common.FcoId,
            AccountNo = common.AccountNo,
            Quantity = common.Quantity,
            Price = Converter.ToStr(Converter.GetProp(data, "price")),
            Symbol = common.Symbol,
            Side = Converter.ToOptStr(Converter.GetProp(data, "side")),
            OrderType = Converter.ToOptStr(Converter.GetProp(data, "orderType")),
            IsMainOrder = Converter.ToBool(Converter.Pick(data, "isMainOrder", "IsMainOrder")),
            IsAttachedOrder = Converter.ToBool(Converter.GetProp(data, "isAttachedOrder")),
            CreatedTime = Converter.ToStr(Converter.GetProp(data, "createdTime")),
            UpdatedTime = Converter.ToStr(Converter.GetProp(data, "updatedTime")),
            UniqueID = Converter.ToStr(Converter.Pick(data, "uniqueId", "uniquedId", "UniqueId")),
            OrderID = Converter.ToStr(Converter.GetProp(data, "orderId")),
            MatchedQuantity = common.MatchedQuantity,
            OSQuantity = Converter.ToInt(Converter.GetProp(data, "osQuantity")),
            AvgPrice = Converter.ToDecimal(Converter.GetProp(data, "avgPrice")),
            Status = Converter.ToOptStr(Converter.GetProp(data, "status")),
            Detail = Converter.ToStr(Converter.GetProp(data, "detail")),
            Type = common.Type,
        };
    }
}

public record FCOOrderBookResponse
{
    public int PageIndex { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int ItemsCount { get; init; }
    public int PagesCount { get; init; }
    public List<FCOOrder> OrderBook { get; init; } = new();

    internal static FCOOrderBookResponse FromJson(JsonElement data)
    {
        var root = Converter.IsNoContent(data) ? default : data;
        var rows = root.ValueKind == JsonValueKind.Array ? root : Converter.Pick(root, "data", "orderBook", "order_book");
        return new FCOOrderBookResponse
        {
            PageIndex = FCOListResponse.PageNumber(root, "pageIndex", 1),
            PageSize = FCOListResponse.PageNumber(root, "pageSize", 10),
            ItemsCount = FCOListResponse.PageNumber(root, "itemsCount", 0),
            PagesCount = Converter.ToInt(Converter.Pick(root, "pagesCount", "pageCount")),
            OrderBook = Converter.GetArray(rows).Where(r => r.ValueKind == JsonValueKind.Object).Select(FCOOrder.FromJson).ToList(),
        };
    }
}

public record FCOPlaceResponse
{
    public string FCOID { get; init; } = string.Empty;

    internal static FCOPlaceResponse FromJson(JsonElement data) =>
        new() { FCOID = Converter.ToStr(Converter.GetProp(Converter.Payload(data), "fcoId")) };
}

public record FCOCancelResponse
{
    public string FCOID { get; init; } = string.Empty;

    internal static FCOCancelResponse FromJson(JsonElement data) =>
        new() { FCOID = Converter.ToStr(Converter.GetProp(Converter.Payload(data), "fcoId")) };
}

/// <summary>One status transition of an FCO.</summary>
public record FCOStatusHistoryItem
{
    public string State { get; init; } = string.Empty;
    public string Time { get; init; } = string.Empty;
    public string? Code { get; init; }
    public string Detail { get; init; } = string.Empty;

    internal static FCOStatusHistoryItem FromJson(JsonElement d) => new()
    {
        State = Converter.ToStr(Converter.GetProp(d, "state")),
        Time = Converter.ToStr(Converter.GetProp(d, "time")),
        Code = Converter.ToOptStr(Converter.GetProp(d, "code")),
        Detail = Converter.ToStr(Converter.GetProp(d, "detail")),
    };

    /// <summary>Parses <c>{"data": [...]}</c>, a bare array, or the server's 204 marker (empty list).</summary>
    internal static List<FCOStatusHistoryItem> FromResponse(JsonElement data)
    {
        if (Converter.IsNoContent(data)) return [];
        var rows = data.ValueKind == JsonValueKind.Array ? data : Converter.GetProp(data, "data") ?? default;
        return Converter.GetArray(rows).Where(r => r.ValueKind == JsonValueKind.Object).Select(FromJson).ToList();
    }
}

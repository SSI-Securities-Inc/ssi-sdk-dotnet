using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Models;

internal static class SM
{
    public static string S(JsonElement el, string name) => Converter.ToStr(Converter.GetProp(el, name));

    public static decimal D(JsonElement el, string name) => Converter.ToDecimal(Converter.GetProp(el, name));

    public static decimal? OptD(JsonElement el, string name) => Converter.ToDecimalOrNull(Converter.GetProp(el, name));

    public static int I(JsonElement el, string name) => Converter.ToInt(Converter.GetProp(el, name));

    public static long L(JsonElement el, string name) => Converter.ToInt64(Converter.GetProp(el, name));

    /// <summary>Split an array of [price, volume] pairs into parallel lists, skipping malformed entries.</summary>
    public static void Levels(JsonElement el, string name, List<decimal> prices, List<int> volumes)
    {
        foreach (var item in Converter.GetArray(Converter.GetProp(el, name)))
        {
            if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() < 2) continue;
            var pair = item.EnumerateArray().Take(2).ToArray();
            prices.Add(Converter.ToDecimal(pair[0]));
            volumes.Add(Converter.ToInt(pair[1]));
        }
    }
}

public sealed class HeartbeatMessage
{
    public string Method { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    internal static HeartbeatMessage FromJson(JsonElement el) => new()
    {
        Method = SM.S(el, "method"),
        Channel = SM.S(el, "channel"),
        Status = SM.S(el, "status"),
        Message = SM.S(el, "message"),
    };
}

public sealed class TradeMessage
{
    public string Type { get; set; } = DataType.Trade;
    public string TradingTime { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Side { get; set; } = "U";
    public int TotalVolume { get; set; }
    public decimal? Open { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal? Avg { get; set; }

    internal static TradeMessage FromJson(JsonElement el) => new()
    {
        TradingTime = SM.S(el, "t"),
        Symbol = SM.S(el, "s"),
        Price = SM.D(el, "p"),
        Quantity = SM.I(el, "q"),
        Side = SM.S(el, "si") is { Length: > 0 } s ? s : "U",
        TotalVolume = SM.I(el, "v"),
        Open = SM.OptD(el, "o"),
        High = SM.OptD(el, "h"),
        Low = SM.OptD(el, "l"),
        Avg = SM.OptD(el, "a"),
    };
}

/// <summary>Tick of an index itself, from <c>trade.index.CODE</c>. A field the server omitted is null.</summary>
public sealed class IndexTickMessage
{
    public string Type { get; set; } = DataType.Trade;
    public string Symbol { get; set; } = string.Empty;
    public string Board { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TradingTime { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public decimal? Change { get; set; }
    public decimal? ChangePercent { get; set; }
    public decimal? Advances { get; set; }
    public decimal? Declines { get; set; }
    public decimal? NoChange { get; set; }
    public decimal? Ceilings { get; set; }
    public decimal? Floors { get; set; }
    public decimal? TotalMatchValue { get; set; }
    public decimal? TotalMatchQuantity { get; set; }
    public decimal? TotalPutValue { get; set; }
    public decimal? TotalPutQuantity { get; set; }
    public decimal? TotalQuantity { get; set; }
    public decimal? TotalValue { get; set; }

    /// <summary>The payload as the server sent it.</summary>
    public Dictionary<string, JsonElement> Raw { get; set; } = new();

    internal static IndexTickMessage FromJson(JsonElement el) => new()
    {
        Symbol = SM.S(el, "s"),
        Board = SM.S(el, "b"),
        Status = SM.S(el, "ss"),
        TradingTime = SM.S(el, "t"),
        Value = SM.OptD(el, "v"),
        Change = SM.OptD(el, "c"),
        ChangePercent = SM.OptD(el, "cr"),
        Advances = SM.OptD(el, "adv"),
        Declines = SM.OptD(el, "dec"),
        NoChange = SM.OptD(el, "nc"),
        Ceilings = SM.OptD(el, "ce"),
        Floors = SM.OptD(el, "fl"),
        TotalMatchValue = SM.OptD(el, "tmv"),
        TotalMatchQuantity = SM.OptD(el, "tmq"),
        TotalPutValue = SM.OptD(el, "tvp"),
        TotalPutQuantity = SM.OptD(el, "tqp"),
        TotalQuantity = SM.OptD(el, "tq"),
        TotalValue = SM.OptD(el, "tv"),
        Raw = Copy(el),
    };

    internal static Dictionary<string, JsonElement> Copy(JsonElement el) =>
        el.ValueKind == JsonValueKind.Object ? el.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone()) : new();
}

public sealed class IntervalMessage
{
    public string Type { get; set; } = DataType.Trade;
    public string IntervalTime { get; set; } = string.Empty;
    public string TradingTime { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public int Volume { get; set; }

    internal static IntervalMessage FromJson(JsonElement el) => new()
    {
        IntervalTime = SM.S(el, "st"),
        TradingTime = SM.S(el, "t"),
        Symbol = SM.S(el, "s"),
        Open = SM.D(el, "o"),
        High = SM.D(el, "h"),
        Low = SM.D(el, "l"),
        Close = SM.D(el, "c"),
        Volume = SM.I(el, "v"),
    };
}

public sealed class QuoteMessage
{
    public string Type { get; set; } = DataType.Quote;
    public string TradingTime { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public List<decimal> BidPrices { get; set; } = [];
    public List<int> BidVolumes { get; set; } = [];
    public List<decimal> AskPrices { get; set; } = [];
    public List<int> AskVolumes { get; set; } = [];

    internal static QuoteMessage FromJson(JsonElement el)
    {
        var qm = new QuoteMessage { TradingTime = SM.S(el, "t"), Symbol = SM.S(el, "s") };
        SM.Levels(el, "bids", qm.BidPrices, qm.BidVolumes);
        SM.Levels(el, "asks", qm.AskPrices, qm.AskVolumes);
        return qm;
    }
}

public sealed class MarketStatusMessage
{
    public string Market { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TradingDate { get; set; } = string.Empty;

    internal static MarketStatusMessage FromJson(JsonElement el) => new()
    {
        Market = SM.S(el, "market"),
        Status = SM.S(el, "status"),
        TradingDate = SM.S(el, "tradingDate"),
    };
}

/// <summary>Market session flag (ATO, LO, ATC, ...) broadcast on the <c>market.flag</c> topic.</summary>
public sealed class MarketFlagMessage
{
    public string Board { get; set; } = string.Empty;
    public string TradingTime { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;

    internal static MarketFlagMessage FromJson(JsonElement el) => new()
    {
        Board = SM.S(el, "b"),
        TradingTime = SM.S(el, "t"),
        Flag = SM.S(el, "f"),
    };
}

/// <summary>Full summary of one index, from <c>indexsummary.CODE</c>; the fields are the server's own keys.</summary>
public sealed class IndexSummaryMessage
{
    public string Index { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Data { get; set; } = new();

    /// <summary>The first of the keys present in <see cref="Data"/>; null when none is.</summary>
    public JsonElement? Get(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (Data.TryGetValue(key, out var value)) return value;
        }
        return null;
    }

    internal static IndexSummaryMessage FromJson(JsonElement data, string topic)
    {
        const string prefix = "indexsummary.";
        var code = topic.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? topic[prefix.Length..] : string.Empty;
        return new IndexSummaryMessage { Index = code.ToUpperInvariant(), Data = IndexTickMessage.Copy(data) };
    }
}

/// <summary>Master data (ceiling, floor, reference price) of a symbol, from <c>market.SYMBOL</c>.</summary>
public sealed class MarketDataMessage
{
    public string Symbol { get; set; } = string.Empty;
    public string Board { get; set; } = string.Empty;
    public string TradingTime { get; set; } = string.Empty;
    public decimal? Ceiling { get; set; }
    public decimal? Floor { get; set; }
    public decimal? RefPrice { get; set; }

    internal static MarketDataMessage FromJson(JsonElement el) => new()
    {
        Symbol = SM.S(el, "s"),
        Board = SM.S(el, "b"),
        TradingTime = SM.S(el, "t"),
        Ceiling = SM.OptD(el, "ce"),
        Floor = SM.OptD(el, "fl"),
        RefPrice = SM.OptD(el, "ref"),
    };
}

public sealed class ForeignRoomMessage
{
    public string Type { get; set; } = DataType.Room;
    public string TradingTime { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int TotalRoom { get; set; }
    public int CurrentRoom { get; set; }
    public int BuyQuantity { get; set; }
    public long BuyValue { get; set; }
    public int SellQuantity { get; set; }
    public long SellValue { get; set; }

    internal static ForeignRoomMessage FromJson(JsonElement el) => new()
    {
        TradingTime = SM.S(el, "t"),
        Symbol = SM.S(el, "s"),
        TotalRoom = SM.I(el, "tr"),
        CurrentRoom = SM.I(el, "cr"),
        BuyQuantity = SM.I(el, "bq"),
        BuyValue = SM.L(el, "bv"),
        SellQuantity = SM.I(el, "sq"),
        SellValue = SM.L(el, "sv"),
    };
}

public sealed class PutMessage
{
    public string Type { get; set; } = DataType.Put;
    public string TradingTime { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int TotalQuantity { get; set; }
    public long TotalValue { get; set; }

    internal static PutMessage FromJson(JsonElement el) => new()
    {
        TradingTime = SM.S(el, "t"),
        Symbol = SM.S(el, "s"),
        Price = SM.D(el, "p"),
        Quantity = SM.I(el, "q"),
        TotalQuantity = SM.I(el, "tq"),
        TotalValue = SM.L(el, "tv"),
    };
}

public sealed class OddLotMessage
{
    public string Type { get; set; } = DataType.OddLot;
    public string TradingTime { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public List<decimal> BidPrices { get; set; } = [];
    public List<int> BidVolumes { get; set; } = [];
    public List<decimal> AskPrices { get; set; } = [];
    public List<int> AskVolumes { get; set; } = [];

    internal static OddLotMessage FromJson(JsonElement el)
    {
        var ol = new OddLotMessage
        {
            TradingTime = SM.S(el, "t"),
            Symbol = SM.S(el, "s"),
            Price = SM.D(el, "p"),
            Quantity = SM.I(el, "q"),
        };
        SM.Levels(el, "bids", ol.BidPrices, ol.BidVolumes);
        SM.Levels(el, "asks", ol.AskPrices, ol.AskVolumes);
        return ol;
    }
}

/// <summary>Order status update (<c>orderEvent</c> on <c>order.ACCOUNT</c>).</summary>
public sealed class OrderStatusMessage
{
    public string Type { get; set; } = StreamingType.Order;
    public string AccountNo { get; set; } = string.Empty;
    public string ClientRequestId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string? Side { get; set; }
    public string? OrderType { get; set; }

    /// <summary>Order price; 0 when the server sent a price word instead (see <see cref="PriceMarker"/>).</summary>
    public decimal Price { get; set; }
    public string? PriceMarker { get; set; }
    public int Quantity { get; set; }
    public int OsQuantity { get; set; }
    public int FilledQuantity { get; set; }
    public int CancelQuantity { get; set; }
    public string? Status { get; set; }
    public string InputTime { get; set; } = string.Empty;
    public string ModifiedTime { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal AvgPrice { get; set; }
    public string RejectReason { get; set; } = string.Empty;
    public string SystemRequestId { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string NotifyId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;

    [Obsolete("Use ModifiedTime.")]
    public string ModifyTime => ModifiedTime;

    internal static OrderStatusMessage FromJson(JsonElement el)
    {
        var (price, marker) = Order.ParsePrice(Converter.GetProp(el, "price"));
        return new OrderStatusMessage
        {
            AccountNo = SM.S(el, "accountNo"),
            ClientRequestId = SM.S(el, "clientRequestId"),
            OrderId = SM.S(el, "orderId"),
            Symbol = SM.S(el, "symbol"),
            Side = PM.Known(Converter.ToOptStr(Converter.GetProp(el, "side")), SsiSdk.OrderSide.Buy, SsiSdk.OrderSide.Sell),
            OrderType = Order.NormalizeOrderType(Converter.ToOptStr(Converter.GetProp(el, "orderType"))),
            Price = price,
            PriceMarker = marker,
            Quantity = SM.I(el, "quantity"),
            OsQuantity = SM.I(el, "osQty"),
            FilledQuantity = SM.I(el, "filledQty"),
            CancelQuantity = SM.I(el, "cancelQty"),
            Status = Order.NormalizeStatus(Converter.ToOptStr(Converter.GetProp(el, "orderStatus"))),
            InputTime = SM.S(el, "inputTime"),
            ModifiedTime = Converter.ToStr(Converter.Pick(el, "modifiedTime", "modifyTime")),
            Message = SM.S(el, "rejectReason"),
            AvgPrice = SM.D(el, "avgPrice"),
            RejectReason = SM.S(el, "rejectReason"),
            SystemRequestId = SM.S(el, "systemRequestId"),
            ErrorCode = SM.S(el, "errorCode"),
            ErrorMessage = SM.S(el, "errorMessage"),
            NotifyId = SM.S(el, "notifyId"),
            ConnectionId = SM.S(el, "connectionId"),
        };
    }
}

/// <summary>A fill of an order (<c>orderMatchEvent</c> on <c>order.ACCOUNT</c>).</summary>
public sealed class OrderMatchMessage
{
    public string Type { get; set; } = StreamingType.OrderMatch;
    public string NotifyId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public string ClientRequestId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string? Side { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    public decimal MatchPrice { get; set; }
    public int MatchQty { get; set; }
    public string MatchTime { get; set; } = string.Empty;

    internal static OrderMatchMessage FromJson(JsonElement el) => new()
    {
        NotifyId = SM.S(el, "notifyId"),
        ConnectionId = SM.S(el, "connectionId"),
        ClientRequestId = SM.S(el, "clientRequestId"),
        OrderId = SM.S(el, "orderId"),
        Symbol = SM.S(el, "symbol"),
        Side = PM.Known(Converter.ToOptStr(Converter.GetProp(el, "side")), SsiSdk.OrderSide.Buy, SsiSdk.OrderSide.Sell),
        AccountNo = SM.S(el, "accountNo"),
        MatchPrice = SM.D(el, "matchPrice"),
        MatchQty = SM.I(el, "matchQty"),
        MatchTime = SM.S(el, "matchTime"),
    };
}

/// <summary>Position update (<c>clientPortfolioEvent</c>, derivative accounts only). Omitted numbers are null.</summary>
public sealed class PortfolioMessage
{
    public string Type { get; set; } = StreamingType.Portfolio;
    public string AccountNo { get; set; } = string.Empty;
    /// <summary>Deprecated: the server never sends it, so it is always null.</summary>
    public decimal? TotalAsset { get; set; }
    /// <summary>Deprecated: the server never sends it, so it is always null.</summary>
    public decimal? CashBalance { get; set; }
    /// <summary>Deprecated: the server never sends it, so it is always null.</summary>
    public decimal? StockValue { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public int LongQty { get; set; }
    public int ShortQty { get; set; }
    public int Net { get; set; }
    public decimal? BidAvgPrice { get; set; }
    public decimal? AskAvgPrice { get; set; }
    public decimal? TradePrice { get; set; }
    public decimal? MarketPrice { get; set; }
    public decimal? FloatingPL { get; set; }
    public decimal? TradingPL { get; set; }
    public string PositionType { get; set; } = string.Empty;

    internal static PortfolioMessage FromJson(JsonElement el) => new()
    {
        AccountNo = SM.S(el, "accountNo"),
        TotalAsset = SM.OptD(el, "totalAsset"),
        CashBalance = SM.OptD(el, "cashBalance"),
        StockValue = SM.OptD(el, "stockValue"),
        Symbol = SM.S(el, "symbol"),
        LongQty = SM.I(el, "longQty"),
        ShortQty = SM.I(el, "shortQty"),
        Net = SM.I(el, "net"),
        BidAvgPrice = SM.OptD(el, "bidAvgPrice"),
        AskAvgPrice = SM.OptD(el, "askAvgPrice"),
        TradePrice = SM.OptD(el, "tradePrice"),
        MarketPrice = SM.OptD(el, "marketPrice"),
        FloatingPL = SM.OptD(el, "floatingPL"),
        TradingPL = SM.OptD(el, "tradingPL"),
        PositionType = SM.S(el, "positionType"),
    };
}

public sealed class FCOOrderUpdateMessage
{
    public string Type { get; set; } = StreamingType.FCOOrder;
    public string FCOID { get; set; } = string.Empty;
    public string? ProcessStatus { get; set; }
    public int MatchedQuantity { get; set; }
    public bool IsPlaceOrder { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Price { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string UpdatedTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? FCOType { get; set; }

    internal static FCOOrderUpdateMessage FromJson(JsonElement el)
    {
        var common = FcoCommon.Parse(el);
        return new FCOOrderUpdateMessage
        {
            FCOID = common.FcoId,
            ProcessStatus = PM.Known(
                Converter.ToOptStr(Converter.GetProp(el, "processStatus")),
                SsiSdk.FCOStatus.Init, SsiSdk.FCOStatus.Wait, SsiSdk.FCOStatus.Tri, SsiSdk.FCOStatus.Trit,
                SsiSdk.FCOStatus.Ter, SsiSdk.FCOStatus.Fis, SsiSdk.FCOStatus.Wc, SsiSdk.FCOStatus.Exp, SsiSdk.FCOStatus.Err),
            MatchedQuantity = common.MatchedQuantity,
            IsPlaceOrder = common.IsPlaceOrder,
            Symbol = common.Symbol,
            Quantity = common.Quantity,
            Price = SM.S(el, "price"),
            AccountNo = common.AccountNo,
            UpdatedTime = SM.S(el, "updatedTime"),
            Status = SM.S(el, "status"),
            Message = SM.S(el, "message"),
            Username = SM.S(el, "username"),
            EventType = SM.S(el, "eventType"),
            FCOType = PM.Known(
                common.Type,
                SsiSdk.FCOType.GTD, SsiSdk.FCOType.Stop, SsiSdk.FCOType.StopLimit, SsiSdk.FCOType.TrailingStop,
                SsiSdk.FCOType.TrailingStopLimit, SsiSdk.FCOType.OCO, SsiSdk.FCOType.BullBear),
        };
    }
}

/// <summary>Turns a received frame into its message model; a frame this SDK has no model for stays a raw JsonElement.</summary>
internal static class StreamingMessageParser
{
    private static JsonElement Payload(JsonElement msg)
    {
        var data = Converter.GetProp(msg, "data");
        return data is { ValueKind: JsonValueKind.Object } d ? d : JsonDocument.Parse("{}").RootElement;
    }

    public static object ParseDataMessage(JsonElement msg)
    {
        var topic = Converter.ToStr(Converter.GetProp(msg, "topic"));
        var data = Payload(msg);
        var kind = topic.ToLowerInvariant();

        if (kind.StartsWith("trade.index.", StringComparison.Ordinal))
            return IsCandle(kind) ? IntervalMessage.FromJson(data) : IndexTickMessage.FromJson(data);
        if (kind.StartsWith(DataTopic.Trade, StringComparison.Ordinal))
            return IsCandle(kind) ? IntervalMessage.FromJson(data) : TradeMessage.FromJson(data);
        if (kind.StartsWith(DataTopic.Quote, StringComparison.Ordinal)) return QuoteMessage.FromJson(data);
        if (kind.StartsWith(DataTopic.Room, StringComparison.Ordinal)) return ForeignRoomMessage.FromJson(data);
        if (kind == Constants.TopicMarketFlag) return MarketFlagMessage.FromJson(data);
        if (kind.StartsWith(DataTopic.IndexSummary, StringComparison.Ordinal)) return IndexSummaryMessage.FromJson(data, topic);
        if (kind.StartsWith(DataTopic.Market, StringComparison.Ordinal))
        {
            var isMasterData = new[] { "s", "ce", "fl", "ref" }.Any(k => data.TryGetProperty(k, out _));
            return isMasterData ? MarketDataMessage.FromJson(data) : MarketStatusMessage.FromJson(data);
        }
        if (kind.StartsWith(DataTopic.Put, StringComparison.Ordinal)) return PutMessage.FromJson(data);
        if (kind.StartsWith(DataTopic.OddLot, StringComparison.Ordinal)) return OddLotMessage.FromJson(data);
        return msg;
    }

    private static bool IsCandle(string kind) => kind.EndsWith("@1m", StringComparison.Ordinal) || kind.EndsWith("@5m", StringComparison.Ordinal);

    public static object ParseTradingMessage(JsonElement msg)
    {
        var topic = Converter.ToStr(Converter.GetProp(msg, "topic"));
        var data = Payload(msg);
        var eventType = Converter.ToOptStr(Converter.GetProp(data, "eventType"));
        if (eventType == "fcoEvent") return FCOOrderUpdateMessage.FromJson(data);
        if (eventType == StreamingType.OrderMatch) return OrderMatchMessage.FromJson(data);
        if (eventType == StreamingType.Portfolio) return PortfolioMessage.FromJson(data);
        if (eventType == StreamingType.Order) return OrderStatusMessage.FromJson(data);
        if (string.IsNullOrEmpty(eventType))
        {
            if (topic.StartsWith("order.", StringComparison.OrdinalIgnoreCase)) return OrderStatusMessage.FromJson(data);
            if (topic.StartsWith("portfolio.", StringComparison.OrdinalIgnoreCase)) return PortfolioMessage.FromJson(data);
        }
        return msg;
    }
}

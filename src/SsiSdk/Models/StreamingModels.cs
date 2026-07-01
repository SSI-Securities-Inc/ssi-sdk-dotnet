using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Models;

public sealed class HeartbeatMessage
{
    public string Method { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    internal static HeartbeatMessage FromJson(JsonElement el) => new()
    {
        Method = Converter.ToStr(Converter.GetProp(el, "method")),
        Channel = Converter.ToStr(Converter.GetProp(el, "channel")),
        Status = Converter.ToStr(Converter.GetProp(el, "status")),
        Message = Converter.ToStr(Converter.GetProp(el, "message")),
    };
}

public sealed class TradeMessage
{
    public string Type { get; set; } = DataType.Trade;
    public string TradingTime { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Quantity { get; set; }
    public string Side { get; set; } = "U";
    public int TotalVolume { get; set; }

    internal static TradeMessage FromJson(JsonElement el) => new()
    {
        TradingTime = Converter.ToStr(Converter.GetProp(el, "t")),
        Symbol = Converter.ToStr(Converter.GetProp(el, "s")),
        Price = Converter.ToFloat64(Converter.GetProp(el, "p")),
        Quantity = Converter.ToInt(Converter.GetProp(el, "q")),
        Side = Converter.ToStr(Converter.GetProp(el, "si")) is { Length: > 0 } s ? s : "U",
        TotalVolume = Converter.ToInt(Converter.GetProp(el, "v")),
    };
}

public sealed class IntervalMessage
{
    public string Type { get; set; } = DataType.Trade;
    public string IntervalTime { get; set; } = string.Empty;
    public string TradingTime { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public int Volume { get; set; }

    internal static IntervalMessage FromJson(JsonElement el) => new()
    {
        IntervalTime = Converter.ToStr(Converter.GetProp(el, "st")),
        TradingTime = Converter.ToStr(Converter.GetProp(el, "t")),
        Symbol = Converter.ToStr(Converter.GetProp(el, "s")),
        Open = Converter.ToFloat64(Converter.GetProp(el, "o")),
        High = Converter.ToFloat64(Converter.GetProp(el, "h")),
        Low = Converter.ToFloat64(Converter.GetProp(el, "l")),
        Close = Converter.ToFloat64(Converter.GetProp(el, "c")),
        Volume = Converter.ToInt(Converter.GetProp(el, "v")),
    };
}

public sealed class QuoteMessage
{
    public string Type { get; set; } = DataType.Quote;
    public string TradingTime { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public List<double> BidPrices { get; set; } = [];
    public List<int> BidVolumes { get; set; } = [];
    public List<double> AskPrices { get; set; } = [];
    public List<int> AskVolumes { get; set; } = [];

    internal static QuoteMessage FromJson(JsonElement el)
    {
        var qm = new QuoteMessage
        {
            TradingTime = Converter.ToStr(Converter.GetProp(el, "t")),
            Symbol = Converter.ToStr(Converter.GetProp(el, "s")),
        };
        ParsePairs(Converter.GetProp(el, "bids"), qm.BidPrices, qm.BidVolumes);
        ParsePairs(Converter.GetProp(el, "asks"), qm.AskPrices, qm.AskVolumes);
        return qm;
    }

    private static void ParsePairs(JsonElement? arr, List<double> prices, List<int> volumes)
    {
        foreach (var item in Converter.GetArray(arr))
        {
            if (item.ValueKind != JsonValueKind.Array) continue;
            var pair = new List<JsonElement>();
            foreach (var p in item.EnumerateArray()) pair.Add(p);
            if (pair.Count < 2) continue;
            prices.Add(Converter.ToFloat64(pair[0]));
            volumes.Add(Converter.ToInt(pair[1]));
        }
    }
}

public sealed class MarketStatusMessage
{
    public string Market { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TradingDate { get; set; } = string.Empty;

    internal static MarketStatusMessage FromJson(JsonElement el) => new()
    {
        Market = Converter.ToStr(Converter.GetProp(el, "market")),
        Status = Converter.ToStr(Converter.GetProp(el, "status")),
        TradingDate = Converter.ToStr(Converter.GetProp(el, "tradingDate")),
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
    public int BuyValue { get; set; }
    public int SellQuantity { get; set; }
    public int SellValue { get; set; }

    internal static ForeignRoomMessage FromJson(JsonElement el) => new()
    {
        TradingTime = Converter.ToStr(Converter.GetProp(el, "t")),
        Symbol = Converter.ToStr(Converter.GetProp(el, "s")),
        TotalRoom = Converter.ToInt(Converter.GetProp(el, "tr")),
        CurrentRoom = Converter.ToInt(Converter.GetProp(el, "cr")),
        BuyQuantity = Converter.ToInt(Converter.GetProp(el, "bq")),
        BuyValue = Converter.ToInt(Converter.GetProp(el, "bv")),
        SellQuantity = Converter.ToInt(Converter.GetProp(el, "sq")),
        SellValue = Converter.ToInt(Converter.GetProp(el, "sv")),
    };
}

public sealed class PutMessage
{
    public string Type { get; set; } = DataType.Put;
    public string TradingTime { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Quantity { get; set; }
    public int TotalQuantity { get; set; }
    public int TotalValue { get; set; }

    internal static PutMessage FromJson(JsonElement el) => new()
    {
        TradingTime = Converter.ToStr(Converter.GetProp(el, "t")),
        Symbol = Converter.ToStr(Converter.GetProp(el, "s")),
        Price = Converter.ToFloat64(Converter.GetProp(el, "p")),
        Quantity = Converter.ToInt(Converter.GetProp(el, "q")),
        TotalQuantity = Converter.ToInt(Converter.GetProp(el, "tq")),
        TotalValue = Converter.ToInt(Converter.GetProp(el, "tv")),
    };
}

public sealed class OddLotMessage
{
    public string Type { get; set; } = DataType.OddLot;
    public string TradingTime { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Quantity { get; set; }
    public List<double> BidPrices { get; set; } = [];
    public List<int> BidVolumes { get; set; } = [];
    public List<double> AskPrices { get; set; } = [];
    public List<int> AskVolumes { get; set; } = [];

    internal static OddLotMessage FromJson(JsonElement el)
    {
        var ol = new OddLotMessage
        {
            TradingTime = Converter.ToStr(Converter.GetProp(el, "t")),
            Symbol = Converter.ToStr(Converter.GetProp(el, "s")),
            Price = Converter.ToFloat64(Converter.GetProp(el, "p")),
            Quantity = Converter.ToInt(Converter.GetProp(el, "q")),
        };
        ParsePairs(Converter.GetProp(el, "bids"), ol.BidPrices, ol.BidVolumes);
        ParsePairs(Converter.GetProp(el, "asks"), ol.AskPrices, ol.AskVolumes);
        return ol;
    }

    private static void ParsePairs(JsonElement? arr, List<double> prices, List<int> volumes)
    {
        foreach (var item in Converter.GetArray(arr))
        {
            if (item.ValueKind != JsonValueKind.Array) continue;
            var pair = new List<JsonElement>();
            foreach (var p in item.EnumerateArray()) pair.Add(p);
            if (pair.Count < 2) continue;
            prices.Add(Converter.ToFloat64(pair[0]));
            volumes.Add(Converter.ToInt(pair[1]));
        }
    }
}

public sealed class OrderStatusMessage
{
    public string Type { get; set; } = StreamingType.Order;
    public string AccountNo { get; set; } = string.Empty;
    public string ClientRequestId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Quantity { get; set; }
    public int OsQuantity { get; set; }
    public int FilledQuantity { get; set; }
    public int CancelQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string InputTime { get; set; } = string.Empty;
    public string ModifyTime { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    internal static OrderStatusMessage FromJson(JsonElement el) => new()
    {
        AccountNo = Converter.ToStr(Converter.GetProp(el, "accountNo")),
        ClientRequestId = Converter.ToStr(Converter.GetProp(el, "clientRequestId")),
        OrderId = Converter.ToStr(Converter.GetProp(el, "orderId")),
        Symbol = Converter.ToStr(Converter.GetProp(el, "symbol")),
        Side = Converter.ToStr(Converter.GetProp(el, "side")),
        OrderType = Converter.ToStr(Converter.GetProp(el, "orderType")),
        Price = Converter.ToFloat64(Converter.GetProp(el, "price")),
        Quantity = Converter.ToInt(Converter.GetProp(el, "quantity")),
        OsQuantity = Converter.ToInt(Converter.GetProp(el, "osQty")),
        FilledQuantity = Converter.ToInt(Converter.GetProp(el, "filledQty")),
        CancelQuantity = Converter.ToInt(Converter.GetProp(el, "cancelQty")),
        Status = Converter.ToStr(Converter.GetProp(el, "orderStatus")),
        InputTime = Converter.ToStr(Converter.GetProp(el, "inputTime")),
        ModifyTime = Converter.ToStr(Converter.GetProp(el, "modifyTime")),
        Message = Converter.ToStr(Converter.GetProp(el, "rejectReason")),
    };
}

public sealed class PortfolioMessage
{
    public string Type { get; set; } = StreamingType.Portfolio;
    public string AccountNo { get; set; } = string.Empty;
    public double TotalAsset { get; set; }
    public double CashBalance { get; set; }
    public double StockValue { get; set; }

    internal static PortfolioMessage FromJson(JsonElement el) => new()
    {
        AccountNo = Converter.ToStr(Converter.GetProp(el, "accountNo")),
        TotalAsset = Converter.ToFloat64(Converter.GetProp(el, "totalAsset")),
        CashBalance = Converter.ToFloat64(Converter.GetProp(el, "cashBalance")),
        StockValue = Converter.ToFloat64(Converter.GetProp(el, "stockValue")),
    };
}

internal static class StreamingMessageParser
{
    public static object ParseDataMessage(string topic, JsonElement data)
    {
        if (topic.StartsWith(DataTopic.Trade))
        {
            return Converter.GetProp(data, "st") is not null
                ? IntervalMessage.FromJson(data)
                : (object)TradeMessage.FromJson(data);
        }
        if (topic.StartsWith(DataTopic.Quote)) return QuoteMessage.FromJson(data);
        if (topic.StartsWith(DataTopic.Room)) return ForeignRoomMessage.FromJson(data);
        if (topic.StartsWith(DataTopic.Market)) return MarketStatusMessage.FromJson(data);
        if (topic.StartsWith(DataTopic.Put)) return PutMessage.FromJson(data);
        if (topic.StartsWith(DataTopic.OddLot)) return OddLotMessage.FromJson(data);
        return data;
    }

    public static object ParseTradingMessage(string topic, JsonElement data)
    {
        if (topic.StartsWith("order.")) return OrderStatusMessage.FromJson(data);
        if (topic.StartsWith("portfolio.")) return PortfolioMessage.FromJson(data);
        return data;
    }
}

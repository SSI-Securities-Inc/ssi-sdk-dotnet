using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Models;

internal static class MarketParse
{
    /// <summary>Known board (any case) gives the canonical name, unknown text is kept as is, blank gives null.</summary>
    public static string? Board(JsonElement? el)
    {
        var text = Converter.ToOptStr(el)?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        foreach (var known in new[] { SsiSdk.Board.HOSE, SsiSdk.Board.HNX, SsiSdk.Board.UPCOM, SsiSdk.Board.DERIVATIVES })
        {
            if (string.Equals(known, text, StringComparison.OrdinalIgnoreCase)) return known;
        }
        return text;
    }

    public static decimal? Dec(JsonElement el, string name) => Converter.ToDecimalOrNull(Converter.GetProp(el, name));
    public static decimal DecOr0(JsonElement el, string name) => Converter.ToDecimal(Converter.GetProp(el, name));
    public static int Int(JsonElement el, string name) => Converter.ToInt(Converter.GetProp(el, name));
    public static int? OptInt(JsonElement el, string name) => Converter.ToOptInt(Converter.GetProp(el, name));
    public static string Str(JsonElement el, string name) => Converter.ToStr(Converter.GetProp(el, name));
    public static string? OptStr(JsonElement el, string name) => Converter.ToOptStr(Converter.GetProp(el, name));
}

public sealed class OhlcData
{
    public string Symbol { get; set; } = string.Empty;
    public string TradingDate { get; set; } = string.Empty;
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public int Volume { get; set; }
    public decimal Value { get; set; }

    internal static OhlcData FromJson(JsonElement el) => new()
    {
        Symbol = MarketParse.Str(el, "symbol"),
        TradingDate = MarketParse.Str(el, "tradingDate"),
        OpenPrice = MarketParse.DecOr0(el, "open"),
        HighPrice = MarketParse.DecOr0(el, "high"),
        LowPrice = MarketParse.DecOr0(el, "low"),
        ClosePrice = MarketParse.DecOr0(el, "close"),
        Volume = MarketParse.Int(el, "volume"),
        Value = MarketParse.DecOr0(el, "value"),
    };

    internal static List<OhlcData> FromRows(IEnumerable<JsonElement> rows) => rows.Select(FromJson).ToList();
}

public sealed class MarketIndexes
{
    public string Index { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;

    /// <summary>The board as the server sent it (null when blank).</summary>
    public string? Board { get; set; }

    internal static MarketIndexes FromJson(JsonElement el) => new()
    {
        Index = MarketParse.Str(el, "index"),
        IndexName = MarketParse.Str(el, "indexName"),
        Board = MarketParse.Board(Converter.GetProp(el, "board")),
    };

    internal static List<MarketIndexes> FromRows(IEnumerable<JsonElement> rows) => rows.Select(FromJson).ToList();
}

public sealed class MarketIndexSummary
{
    public string Index { get; set; } = string.Empty;
    public string Board { get; set; } = string.Empty;
    public string TradingDate { get; set; } = string.Empty;
    public int TotalTrade { get; set; }
    public decimal TotalTradeValue { get; set; }
    public int TotalMatch { get; set; }
    public decimal TotalMatchValue { get; set; }
    public int TotalDeal { get; set; }
    public decimal TotalDealValue { get; set; }
    public decimal IndexChange { get; set; }
    public decimal IndexChangePercent { get; set; }
    public decimal IndexValue { get; set; }
    public int TotalAdvanceStock { get; set; }
    public int TotalDeclineStock { get; set; }
    public int TotalSteadyStock { get; set; }
    public int TotalCeilingStock { get; set; }
    public int TotalFloorStock { get; set; }
    public int TotalPropBuy { get; set; }
    public decimal TotalPropBuyValue { get; set; }
    public int TotalPropSell { get; set; }
    public decimal TotalPropSellValue { get; set; }
    public decimal? TotalBuyForeign { get; set; }
    public decimal? TotalBuyForeignValue { get; set; }
    public decimal? TotalSellForeign { get; set; }
    public decimal? TotalSellForeignValue { get; set; }
    public decimal? NetPurchasesForeignVolume { get; set; }
    public decimal? NetPurchasesForeignValue { get; set; }

    internal static MarketIndexSummary FromJson(JsonElement el) => new()
    {
        Index = MarketParse.Str(el, "index"),
        Board = MarketParse.Str(el, "board"),
        TradingDate = MarketParse.Str(el, "tradingDate"),
        TotalTrade = MarketParse.Int(el, "totalTrade"),
        TotalTradeValue = MarketParse.DecOr0(el, "totalTradeValue"),
        TotalMatch = MarketParse.Int(el, "totalMatch"),
        TotalMatchValue = MarketParse.DecOr0(el, "totalMatchValue"),
        TotalDeal = MarketParse.Int(el, "totalDeal"),
        TotalDealValue = MarketParse.DecOr0(el, "totalDealValue"),
        IndexChange = MarketParse.DecOr0(el, "indexChange"),
        IndexChangePercent = MarketParse.DecOr0(el, "indexChangePercentage"),
        IndexValue = MarketParse.DecOr0(el, "indexValue"),
        TotalAdvanceStock = MarketParse.Int(el, "totalAdvanceStock"),
        TotalDeclineStock = MarketParse.Int(el, "totalDeclineStock"),
        TotalSteadyStock = MarketParse.Int(el, "totalNoChangeStock"),
        TotalCeilingStock = MarketParse.Int(el, "totalCeilingStock"),
        TotalFloorStock = MarketParse.Int(el, "totalFloorStock"),
        TotalPropBuy = MarketParse.Int(el, "totalPropBuy"),
        TotalPropBuyValue = MarketParse.DecOr0(el, "totalPropBuyValue"),
        TotalPropSell = MarketParse.Int(el, "totalPropSell"),
        TotalPropSellValue = MarketParse.DecOr0(el, "totalPropSellValue"),
        TotalBuyForeign = MarketParse.Dec(el, "totalBuyForeign"),
        TotalBuyForeignValue = MarketParse.Dec(el, "totalBuyForeignValue"),
        TotalSellForeign = MarketParse.Dec(el, "totalSellForeign"),
        TotalSellForeignValue = MarketParse.Dec(el, "totalSellForeignValue"),
        NetPurchasesForeignVolume = MarketParse.Dec(el, "netPurchasesForeignVolume"),
        NetPurchasesForeignValue = MarketParse.Dec(el, "netPurchasesForeignValue"),
    };

    internal static List<MarketIndexSummary> FromRows(IEnumerable<JsonElement> rows) => rows.Select(FromJson).ToList();
}

public sealed class SecuritiesInfo
{
    public string Symbol { get; set; } = string.Empty;
    public string? Board { get; set; }
    public string Index { get; set; } = string.Empty;
    public string SymbolNameVi { get; set; } = string.Empty;
    public string SymbolNameEn { get; set; } = string.Empty;

    /// <summary>Null when the server did not send it (it used to read as 0).</summary>
    public int? LotSize { get; set; }
    public string? MaturityDate { get; set; }
    public string? FirstTradingDate { get; set; }
    public string? LastTradingDate { get; set; }
    public string? CwUnderlyingSymbol { get; set; }
    public decimal? CwExercisePrice { get; set; }
    public decimal? CwExecutionRatio { get; set; }

    /// <summary>Null when the server did not send it (it used to read as 0).</summary>
    public int? ListedShares { get; set; }
    public string? IcbCode { get; set; }
    public string? IcbName { get; set; }
    public decimal? IIndex { get; set; }
    public decimal? INav { get; set; }
    public decimal? OpenInterest { get; set; }
    public decimal? SettlementPrice { get; set; }
    public string? StockType { get; set; }

    internal static SecuritiesInfo FromJson(JsonElement el) => new()
    {
        Symbol = MarketParse.Str(el, "symbol"),
        Board = MarketParse.Board(Converter.GetProp(el, "board")),
        Index = MarketParse.Str(el, "index"),
        SymbolNameVi = MarketParse.Str(el, "symbolNameVi"),
        SymbolNameEn = MarketParse.Str(el, "symbolNameEn"),
        LotSize = MarketParse.OptInt(el, "lotSize"),
        MaturityDate = MarketParse.OptStr(el, "maturityDate"),
        FirstTradingDate = MarketParse.OptStr(el, "firstTradingDate"),
        LastTradingDate = MarketParse.OptStr(el, "lastTradingDate"),
        CwUnderlyingSymbol = MarketParse.OptStr(el, "cwUnderlyingSymbol"),
        CwExercisePrice = MarketParse.Dec(el, "cwExercisePrice"),
        CwExecutionRatio = MarketParse.Dec(el, "cwExecutionRatio"),
        ListedShares = MarketParse.OptInt(el, "listedShare"),
        IcbCode = MarketParse.OptStr(el, "icbCode"),
        IcbName = MarketParse.OptStr(el, "icbName"),
        IIndex = MarketParse.Dec(el, "iIndex"),
        INav = MarketParse.Dec(el, "iNav"),
        OpenInterest = MarketParse.Dec(el, "openInterest"),
        SettlementPrice = MarketParse.Dec(el, "settlementPrice"),
        StockType = MarketParse.OptStr(el, "stockType"),
    };

    internal static List<SecuritiesInfo> FromRows(IEnumerable<JsonElement> rows) => rows.Select(FromJson).ToList();
}

public sealed class SecuritiesSummary
{
    public string Symbol { get; set; } = string.Empty;
    public string TradingDate { get; set; } = string.Empty;
    public decimal PriceChange { get; set; }
    public decimal PriceChangePercent { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal AveragePrice { get; set; }
    public int TotalMatch { get; set; }
    public decimal TotalMatchValue { get; set; }
    public int TotalBuy { get; set; }
    public decimal TotalTradeBuy { get; set; }
    public int TotalSell { get; set; }
    public decimal TotalTradeSell { get; set; }
    public int TotalForeignBuy { get; set; }
    public decimal TotalForeignBuyValue { get; set; }
    public int TotalForeignSell { get; set; }
    public decimal TotalForeignSellValue { get; set; }
    public int RemainForeignRoom { get; set; }
    public int TotalForeignRoom { get; set; }
    public int TotalDeal { get; set; }
    public decimal TotalDealValue { get; set; }
    public decimal OpenInterest { get; set; }
    public decimal SettlementPrice { get; set; }
    public decimal? Ceiling { get; set; }
    public decimal? Floor { get; set; }
    public decimal? RefPrice { get; set; }

    internal static SecuritiesSummary FromJson(JsonElement el) => new()
    {
        Symbol = MarketParse.Str(el, "symbol"),
        TradingDate = MarketParse.Str(el, "tradingDate"),
        PriceChange = MarketParse.DecOr0(el, "priceChange"),
        PriceChangePercent = MarketParse.DecOr0(el, "priceChangePercentage"),
        OpenPrice = MarketParse.DecOr0(el, "open"),
        HighPrice = MarketParse.DecOr0(el, "high"),
        LowPrice = MarketParse.DecOr0(el, "low"),
        ClosePrice = MarketParse.DecOr0(el, "close"),
        AveragePrice = MarketParse.DecOr0(el, "average"),
        TotalMatch = MarketParse.Int(el, "totalMatch"),
        TotalMatchValue = MarketParse.DecOr0(el, "totalMatchValue"),
        TotalBuy = MarketParse.Int(el, "totalBuy"),
        TotalTradeBuy = MarketParse.DecOr0(el, "totalTradeBuy"),
        TotalSell = MarketParse.Int(el, "totalSell"),
        TotalTradeSell = MarketParse.DecOr0(el, "totalTradeSell"),
        TotalForeignBuy = MarketParse.Int(el, "totalForeignBuy"),
        TotalForeignBuyValue = MarketParse.DecOr0(el, "totalForeignBuyValue"),
        TotalForeignSell = MarketParse.Int(el, "totalForeignSell"),
        TotalForeignSellValue = MarketParse.DecOr0(el, "totalForeignSellValue"),
        RemainForeignRoom = MarketParse.Int(el, "remainForeignRoom"),
        TotalForeignRoom = MarketParse.Int(el, "totalForeignRoom"),
        TotalDeal = MarketParse.Int(el, "totalDeal"),
        TotalDealValue = MarketParse.DecOr0(el, "totalDealValue"),
        OpenInterest = MarketParse.DecOr0(el, "openInterest"),
        SettlementPrice = MarketParse.DecOr0(el, "settlementPrice"),
        Ceiling = MarketParse.Dec(el, "ceiling"),
        Floor = MarketParse.Dec(el, "floor"),
        RefPrice = MarketParse.Dec(el, "refPrice"),
    };

    internal static List<SecuritiesSummary> FromRows(IEnumerable<JsonElement> rows) => rows.Select(FromJson).ToList();
}

public sealed class MasterData
{
    public string? Board { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string TradingDate { get; set; } = string.Empty;
    public decimal Ceiling { get; set; }
    public decimal Floor { get; set; }
    public decimal RefPrice { get; set; }

    internal static MasterData FromJson(JsonElement el) => new()
    {
        Board = MarketParse.Board(Converter.GetProp(el, "board")),
        Symbol = MarketParse.Str(el, "symbol"),
        TradingDate = MarketParse.Str(el, "tradingDate"),
        Ceiling = MarketParse.DecOr0(el, "ceiling"),
        Floor = MarketParse.DecOr0(el, "floor"),
        RefPrice = Converter.ToDecimal(Converter.Pick(el, "refPrice", "referencePrice")),
    };

    internal static List<MasterData> FromRows(IEnumerable<JsonElement> rows) => rows.Select(FromJson).ToList();
}

/// <summary>
/// One corporate action (dividend, bonus shares, rights issue, ...) of a security. Every field is the text the server
/// sends: dates stay <c>YYYY/MM/DD</c> text and <see cref="Ratio"/> is not parsed, because its meaning depends on the
/// event type. A field the server leaves blank or omits is <c>""</c>.
/// </summary>
public sealed class CorporateAction
{
    public string Symbol { get; set; } = string.Empty;
    public string EventTitle { get; set; } = string.Empty;
    public string ExrightDate { get; set; } = string.Empty;
    public string RecordDate { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string Ratio { get; set; } = string.Empty;

    internal static CorporateAction FromJson(JsonElement el) => new()
    {
        Symbol = MarketParse.Str(el, "symbol"),
        EventTitle = MarketParse.Str(el, "eventTitle"),
        ExrightDate = MarketParse.Str(el, "exrightDate"),
        RecordDate = MarketParse.Str(el, "recordDate"),
        IssueDate = MarketParse.Str(el, "issueDate"),
        Exchange = MarketParse.Str(el, "exchange"),
        Ratio = MarketParse.Str(el, "ratio"),
    };

    internal static List<CorporateAction> FromRows(IEnumerable<JsonElement> rows) => rows.Select(FromJson).ToList();
}

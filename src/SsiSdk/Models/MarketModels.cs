using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Models;

public sealed class OhlcData
{
    public string Symbol { get; set; } = string.Empty;
    public string TradingDate { get; set; } = string.Empty;
    public double OpenPrice { get; set; }
    public double HighPrice { get; set; }
    public double LowPrice { get; set; }
    public double ClosePrice { get; set; }
    public int Volume { get; set; }
    public double Value { get; set; }

    internal static OhlcData FromJson(JsonElement el) => new()
    {
        Symbol = Converter.ToStr(Converter.GetProp(el, "symbol")),
        TradingDate = Converter.ToStr(Converter.GetProp(el, "tradingDate")),
        OpenPrice = Converter.ToFloat64(Converter.GetProp(el, "open")),
        HighPrice = Converter.ToFloat64(Converter.GetProp(el, "high")),
        LowPrice = Converter.ToFloat64(Converter.GetProp(el, "low")),
        ClosePrice = Converter.ToFloat64(Converter.GetProp(el, "close")),
        Volume = Converter.ToInt(Converter.GetProp(el, "volume")),
        Value = Converter.ToFloat64(Converter.GetProp(el, "value")),
    };

    internal static List<OhlcData> FromJsonArray(JsonElement? el) =>
        Converter.GetArray(el).Select(FromJson).ToList();
}

public sealed class MarketIndexes
{
    public string Index { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public string? Board { get; set; }

    internal static MarketIndexes FromJson(JsonElement el) => new()
    {
        Index = Converter.ToStr(Converter.GetProp(el, "index")),
        IndexName = Converter.ToStr(Converter.GetProp(el, "indexName")),
        Board = Converter.ToStr(Converter.GetProp(el, "board")) is { Length: > 0 } b ? b : null,
    };

    internal static List<MarketIndexes> FromJsonArray(JsonElement? el) =>
        Converter.GetArray(el).Select(FromJson).ToList();
}

public sealed class MarketIndexSummary
{
    public string Index { get; set; } = string.Empty;
    public string Board { get; set; } = string.Empty;
    public string TradingDate { get; set; } = string.Empty;
    public int TotalTrade { get; set; }
    public double TotalTradeValue { get; set; }
    public int TotalMatch { get; set; }
    public double TotalMatchValue { get; set; }
    public int TotalDeal { get; set; }
    public double TotalDealValue { get; set; }
    public double IndexChange { get; set; }
    public double IndexChangePercent { get; set; }
    public double IndexValue { get; set; }
    public int TotalAdvanceStock { get; set; }
    public int TotalDeclineStock { get; set; }
    public int TotalSteadyStock { get; set; }
    public int TotalCeilingStock { get; set; }
    public int TotalFloorStock { get; set; }
    public int TotalPropBuy { get; set; }
    public double TotalPropBuyValue { get; set; }
    public int TotalPropSell { get; set; }
    public double TotalPropSellValue { get; set; }

    internal static MarketIndexSummary FromJson(JsonElement el) => new()
    {
        Index = Converter.ToStr(Converter.GetProp(el, "index")),
        Board = Converter.ToStr(Converter.GetProp(el, "board")),
        TradingDate = Converter.ToStr(Converter.GetProp(el, "tradingDate")),
        TotalTrade = Converter.ToInt(Converter.GetProp(el, "totalTrade")),
        TotalTradeValue = Converter.ToFloat64(Converter.GetProp(el, "totalTradeValue")),
        TotalMatch = Converter.ToInt(Converter.GetProp(el, "totalMatch")),
        TotalMatchValue = Converter.ToFloat64(Converter.GetProp(el, "totalMatchValue")),
        TotalDeal = Converter.ToInt(Converter.GetProp(el, "totalDeal")),
        TotalDealValue = Converter.ToFloat64(Converter.GetProp(el, "totalDealValue")),
        IndexChange = Converter.ToFloat64(Converter.GetProp(el, "indexChange")),
        IndexChangePercent = Converter.ToFloat64(Converter.GetProp(el, "indexChangePercentage")),
        IndexValue = Converter.ToFloat64(Converter.GetProp(el, "indexValue")),
        TotalAdvanceStock = Converter.ToInt(Converter.GetProp(el, "totalAdvanceStock")),
        TotalDeclineStock = Converter.ToInt(Converter.GetProp(el, "totalDeclineStock")),
        TotalSteadyStock = Converter.ToInt(Converter.GetProp(el, "totalNoChangeStock")),
        TotalCeilingStock = Converter.ToInt(Converter.GetProp(el, "totalCeilingStock")),
        TotalFloorStock = Converter.ToInt(Converter.GetProp(el, "totalFloorStock")),
        TotalPropBuy = Converter.ToInt(Converter.GetProp(el, "totalPropBuy")),
        TotalPropBuyValue = Converter.ToFloat64(Converter.GetProp(el, "totalPropBuyValue")),
        TotalPropSell = Converter.ToInt(Converter.GetProp(el, "totalPropSell")),
        TotalPropSellValue = Converter.ToFloat64(Converter.GetProp(el, "totalPropSellValue")),
    };

    internal static List<MarketIndexSummary> FromJsonArray(JsonElement? el) =>
        Converter.GetArray(el).Select(FromJson).ToList();
}

public sealed class SecuritiesInfo
{
    public string Symbol { get; set; } = string.Empty;
    public string? Board { get; set; }
    public string Index { get; set; } = string.Empty;
    public string SymbolNameVi { get; set; } = string.Empty;
    public string SymbolNameEn { get; set; } = string.Empty;
    public int LotSize { get; set; }
    public string? MaturityDate { get; set; }
    public string? FirstTradingDate { get; set; }
    public string? LastTradingDate { get; set; }
    public string? CwUnderlyingSymbol { get; set; }
    public double? CwExercisePrice { get; set; }
    public double? CwExecutionRatio { get; set; }
    public int ListedShares { get; set; }
    public string? IcbCode { get; set; }
    public string? IcbName { get; set; }
    public double? IIndex { get; set; }
    public double? INav { get; set; }

    internal static SecuritiesInfo FromJson(JsonElement el)
    {
        var si = new SecuritiesInfo
        {
            Symbol = Converter.ToStr(Converter.GetProp(el, "symbol")),
            Index = Converter.ToStr(Converter.GetProp(el, "index")),
            SymbolNameVi = Converter.ToStr(Converter.GetProp(el, "symbolNameVi")),
            SymbolNameEn = Converter.ToStr(Converter.GetProp(el, "symbolNameEn")),
            LotSize = Converter.ToInt(Converter.GetProp(el, "lotSize")),
            ListedShares = Converter.ToInt(Converter.GetProp(el, "listedShares")),
        };

        var board = Converter.ToStr(Converter.GetProp(el, "board"));
        if (!string.IsNullOrEmpty(board)) si.Board = board;

        var s = Converter.ToStr(Converter.GetProp(el, "maturityDate"));
        if (!string.IsNullOrEmpty(s)) si.MaturityDate = s;
        s = Converter.ToStr(Converter.GetProp(el, "firstTradingDate"));
        if (!string.IsNullOrEmpty(s)) si.FirstTradingDate = s;
        s = Converter.ToStr(Converter.GetProp(el, "lastTradingDate"));
        if (!string.IsNullOrEmpty(s)) si.LastTradingDate = s;
        s = Converter.ToStr(Converter.GetProp(el, "cwUnderlyingSymbol"));
        if (!string.IsNullOrEmpty(s)) si.CwUnderlyingSymbol = s;
        s = Converter.ToStr(Converter.GetProp(el, "icbCode"));
        if (!string.IsNullOrEmpty(s)) si.IcbCode = s;
        s = Converter.ToStr(Converter.GetProp(el, "icbName"));
        if (!string.IsNullOrEmpty(s)) si.IcbName = s;

        var prop = Converter.GetProp(el, "cwExercisePrice");
        if (prop is not null && prop.Value.ValueKind != JsonValueKind.Null)
            si.CwExercisePrice = Converter.ToFloat64(prop);
        prop = Converter.GetProp(el, "cwExecutionRatio");
        if (prop is not null && prop.Value.ValueKind != JsonValueKind.Null)
            si.CwExecutionRatio = Converter.ToFloat64(prop);
        prop = Converter.GetProp(el, "iIndex");
        if (prop is not null && prop.Value.ValueKind != JsonValueKind.Null)
            si.IIndex = Converter.ToFloat64(prop);
        prop = Converter.GetProp(el, "iNav");
        if (prop is not null && prop.Value.ValueKind != JsonValueKind.Null)
            si.INav = Converter.ToFloat64(prop);

        return si;
    }

    internal static List<SecuritiesInfo> FromJsonArray(JsonElement? el) =>
        Converter.GetArray(el).Select(FromJson).ToList();
}

public sealed class SecuritiesSummary
{
    public string Symbol { get; set; } = string.Empty;
    public string TradingDate { get; set; } = string.Empty;
    public double PriceChange { get; set; }
    public double PriceChangePercent { get; set; }
    public double OpenPrice { get; set; }
    public double HighPrice { get; set; }
    public double LowPrice { get; set; }
    public double ClosePrice { get; set; }
    public double AveragePrice { get; set; }
    public int TotalMatch { get; set; }
    public double TotalMatchValue { get; set; }
    public int TotalBuy { get; set; }
    public double TotalTradeBuy { get; set; }
    public int TotalSell { get; set; }
    public double TotalTradeSell { get; set; }

    internal static SecuritiesSummary FromJson(JsonElement el) => new()
    {
        Symbol = Converter.ToStr(Converter.GetProp(el, "symbol")),
        TradingDate = Converter.ToStr(Converter.GetProp(el, "tradingDate")),
        PriceChange = Converter.ToFloat64(Converter.GetProp(el, "priceChange")),
        PriceChangePercent = Converter.ToFloat64(Converter.GetProp(el, "priceChangePercentage")),
        OpenPrice = Converter.ToFloat64(Converter.GetProp(el, "open")),
        HighPrice = Converter.ToFloat64(Converter.GetProp(el, "high")),
        LowPrice = Converter.ToFloat64(Converter.GetProp(el, "low")),
        ClosePrice = Converter.ToFloat64(Converter.GetProp(el, "close")),
        AveragePrice = Converter.ToFloat64(Converter.GetProp(el, "average")),
        TotalMatch = Converter.ToInt(Converter.GetProp(el, "totalMatch")),
        TotalMatchValue = Converter.ToFloat64(Converter.GetProp(el, "totalMatchValue")),
        TotalBuy = Converter.ToInt(Converter.GetProp(el, "totalBuy")),
        TotalTradeBuy = Converter.ToFloat64(Converter.GetProp(el, "totalTradeBuy")),
        TotalSell = Converter.ToInt(Converter.GetProp(el, "totalSell")),
        TotalTradeSell = Converter.ToFloat64(Converter.GetProp(el, "totalTradeSell")),
    };

    internal static List<SecuritiesSummary> FromJsonArray(JsonElement? el) =>
        Converter.GetArray(el).Select(FromJson).ToList();
}

public sealed class DownloadData
{
    public List<Dictionary<string, object>> Data { get; set; } = [];
    public int TotalCount { get; set; }

    internal static DownloadData FromJson(JsonElement el)
    {
        var result = new DownloadData();
        var dataProp = Converter.GetProp(el, "data");
        if (dataProp is not null && dataProp.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in dataProp.Value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in item.EnumerateObject())
                    {
                        switch (prop.Value.ValueKind)
                        {
                            case JsonValueKind.String:
                                dict[prop.Name] = prop.Value.GetString() ?? string.Empty;
                                break;
                            case JsonValueKind.Number:
                                if (prop.Value.TryGetInt64(out var l)) dict[prop.Name] = l;
                                else dict[prop.Name] = prop.Value.GetDouble();
                                break;
                            case JsonValueKind.True:
                                dict[prop.Name] = true;
                                break;
                            case JsonValueKind.False:
                                dict[prop.Name] = false;
                                break;
                            default:
                                dict[prop.Name] = prop.Value.ToString();
                                break;
                        }
                    }
                    result.Data.Add(dict);
                }
            }
        }
        var totalProp = Converter.GetProp(el, "totalCount");
        result.TotalCount = totalProp is not null ? Converter.ToInt(totalProp.Value) : result.Data.Count;
        return result;
    }
}

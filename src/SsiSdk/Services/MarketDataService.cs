using System.Text.Json;
using SsiSdk.Internal;
using SsiSdk.Models;
using SsiSdk.Transport;

namespace SsiSdk.Services;

public sealed class MarketDataService
{
    private static readonly Logger Log = new("ssi_sdk.services.market");
    private readonly RestClient _rest;

    internal MarketDataService(RestClient rest) => _rest = rest;

    private async Task<List<OhlcData>> GetOhlcAsync(
        string symbol, string timeframe, string from, string to,
        int page = Constants.DefaultPage, int size = Constants.DefaultSize,
        CancellationToken ct = default)
    {
        var p = new Dictionary<string, string>
        {
            ["symbol"] = symbol,
            ["timeFrame"] = timeframe,
            ["from"] = from,
            ["to"] = to,
            ["pageIndex"] = page.ToString(),
            ["pageSize"] = size.ToString(),
        };

        var data = await _rest.GetAsync(Constants.EpDataOhlc, p, ct: ct);
        return OhlcData.FromJsonArray(Converter.GetProp(data, "data"));
    }

    public Task<DownloadData> DownloadOhlc1MinuteAsync(string symbol, CancellationToken ct = default)
    {
        throw new NotImplementedException("OHLC download is not implemented yet");
    }

    public Task<DownloadData> DownloadOhlc1DayAsync(string symbol, CancellationToken ct = default)
    {
        throw new NotImplementedException("OHLC download is not implemented yet");
    }

    public Task<List<OhlcData>> GetOhlc1MinuteAsync(string symbol, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        return GetOhlcAsync(symbol, Timeframe.Minute1, IdGenerator.BeginningOfDay(), IdGenerator.EndOfDay(), ct: ct);
    }

    public Task<List<OhlcData>> GetOhlc1MinuteHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        return GetOhlcAsync(symbol, Timeframe.Minute1, fromDate, toDate, page, size, ct);
    }

    public Task<List<OhlcData>> GetOhlc3MinuteAsync(string symbol, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        return GetOhlcAsync(symbol, Timeframe.Minute3, IdGenerator.BeginningOfDay(), IdGenerator.EndOfDay(), ct: ct);
    }

    public Task<List<OhlcData>> GetOhlc3MinuteHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        return GetOhlcAsync(symbol, Timeframe.Minute3, fromDate, toDate, page, size, ct);
    }

    public Task<List<OhlcData>> GetOhlc5MinuteAsync(string symbol, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        return GetOhlcAsync(symbol, Timeframe.Minute5, IdGenerator.BeginningOfDay(), IdGenerator.EndOfDay(), ct: ct);
    }

    public Task<List<OhlcData>> GetOhlc5MinuteHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        return GetOhlcAsync(symbol, Timeframe.Minute5, fromDate, toDate, page, size, ct);
    }

    public Task<List<OhlcData>> GetOhlc15MinuteAsync(string symbol, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        return GetOhlcAsync(symbol, Timeframe.Minute15, IdGenerator.BeginningOfDay(), IdGenerator.EndOfDay(), ct: ct);
    }

    public Task<List<OhlcData>> GetOhlc15MinuteHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        return GetOhlcAsync(symbol, Timeframe.Minute15, fromDate, toDate, page, size, ct);
    }

    public Task<List<OhlcData>> GetOhlc1HourAsync(string symbol, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        return GetOhlcAsync(symbol, Timeframe.Hour1, IdGenerator.BeginningOfDay(), IdGenerator.EndOfDay(), ct: ct);
    }

    public Task<List<OhlcData>> GetOhlc1HourHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        return GetOhlcAsync(symbol, Timeframe.Hour1, fromDate, toDate, page, size, ct);
    }

    public Task<List<OhlcData>> GetOhlc1DayHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        return GetOhlcAsync(symbol, Timeframe.Day1, fromDate, toDate, page, size, ct);
    }

    public Task<List<OhlcData>> GetOhlc1WeekHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        return GetOhlcAsync(symbol, Timeframe.Week1, fromDate, toDate, page, size, ct);
    }

    public Task<List<OhlcData>> GetOhlc1MonthHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        return GetOhlcAsync(symbol, Timeframe.Month1, fromDate, toDate, page, size, ct);
    }

    public async Task<List<MarketIndexes>> GetIndexesAsync(CancellationToken ct = default)
    {
        var data = await _rest.GetAsync(Constants.EpDataIndexList, ct: ct);
        return MarketIndexes.FromJsonArray(Converter.GetProp(data, "data"));
    }

    public async Task<List<MarketIndexes>> GetIndexesByBoardAsync(string board, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(board, "board");
        var p = new Dictionary<string, string> { ["board"] = board };
        var data = await _rest.GetAsync(Constants.EpDataIndexList, p, ct: ct);
        return MarketIndexes.FromJsonArray(Converter.GetProp(data, "data"));
    }

    public async Task<MarketIndexSummary?> GetIndexSummaryAsync(string index, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(index, "index");
        var p = new Dictionary<string, string> { ["index"] = index };
        var data = await _rest.GetAsync(Constants.EpDataIndexSummary, p, ct: ct);
        var list = MarketIndexSummary.FromJsonArray(Converter.GetProp(data, "data"));
        return list.Count > 0 ? list[0] : null;
    }

    public async Task<MarketIndexSummary?> GetIndexSummaryHistoricalAsync(string index, string tradingDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(index, "index");
        Validate.RequireNonEmpty(tradingDate, "tradingDate");
        var p = new Dictionary<string, string> { ["index"] = index, ["tradingDate"] = tradingDate };
        var data = await _rest.GetAsync(Constants.EpDataIndexSummary, p, ct: ct);
        var list = MarketIndexSummary.FromJsonArray(Converter.GetProp(data, "data"));
        return list.Count > 0 ? list[0] : null;
    }

    public async Task<MarketIndexSummary?> GetBoardSummaryAsync(string board, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(board, "board");
        var p = new Dictionary<string, string> { ["board"] = board };
        var data = await _rest.GetAsync(Constants.EpDataIndexSummary, p, ct: ct);
        var list = MarketIndexSummary.FromJsonArray(Converter.GetProp(data, "data"));
        return list.Count > 0 ? list[0] : null;
    }

    public async Task<MarketIndexSummary?> GetBoardSummaryHistoricalAsync(string board, string tradingDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(board, "board");
        Validate.RequireNonEmpty(tradingDate, "tradingDate");
        var p = new Dictionary<string, string> { ["board"] = board, ["tradingDate"] = tradingDate };
        var data = await _rest.GetAsync(Constants.EpDataIndexSummary, p, ct: ct);
        var list = MarketIndexSummary.FromJsonArray(Converter.GetProp(data, "data"));
        return list.Count > 0 ? list[0] : null;
    }

    public async Task<SecuritiesInfo?> GetSecuritiesInfoAsync(string symbol, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        var p = new Dictionary<string, string> { ["symbol"] = symbol };
        var data = await _rest.GetAsync(Constants.EpDataSecuritiesByBoard, p, ct: ct);
        var list = SecuritiesInfo.FromJsonArray(Converter.GetProp(data, "data"));
        return list.Count > 0 ? list[0] : null;
    }

    public async Task<List<SecuritiesInfo>> GetSecuritiesInfoByIndexAsync(string index, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(index, "index");
        var p = new Dictionary<string, string> { ["index"] = index };
        var data = await _rest.GetAsync(Constants.EpDataSecuritiesByBoard, p, ct: ct);
        return SecuritiesInfo.FromJsonArray(Converter.GetProp(data, "data"));
    }

    public async Task<List<SecuritiesInfo>> GetSecuritiesInfoByBoardAsync(string board, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(board, "board");
        var p = new Dictionary<string, string> { ["board"] = board };
        var data = await _rest.GetAsync(Constants.EpDataSecuritiesByBoard, p, ct: ct);
        return SecuritiesInfo.FromJsonArray(Converter.GetProp(data, "data"));
    }

    public async Task<List<SecuritiesSummary>> GetSecuritiesSummaryAsync(string symbol, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        var today = IdGenerator.TodayDateStr();
        var p = new Dictionary<string, string>
        {
            ["symbol"] = symbol,
            ["from"] = today,
            ["to"] = today,
            ["pageIndex"] = Constants.DefaultPage.ToString(),
            ["pageSize"] = Constants.DefaultSize.ToString(),
        };
        var data = await _rest.GetAsync(Constants.EpDataSecuritiesSummary, p, ct: ct);
        return SecuritiesSummary.FromJsonArray(Converter.GetProp(data, "data"));
    }

    public async Task<List<SecuritiesSummary>> GetSecuritiesSummaryHistoricalAsync(string symbol, string fromDate, string toDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        var p = new Dictionary<string, string>
        {
            ["symbol"] = symbol,
            ["from"] = fromDate,
            ["to"] = toDate,
            ["pageIndex"] = Constants.DefaultPage.ToString(),
            ["pageSize"] = Constants.DefaultSize.ToString(),
        };
        var data = await _rest.GetAsync(Constants.EpDataSecuritiesSummary, p, ct: ct);
        return SecuritiesSummary.FromJsonArray(Converter.GetProp(data, "data"));
    }

    public async Task<List<SecuritiesSummary>> GetSecuritiesSummaryByIndexAsync(string index, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(index, "index");
        var today = IdGenerator.TodayDateStr();
        var p = new Dictionary<string, string>
        {
            ["index"] = index,
            ["from"] = today,
            ["to"] = today,
            ["pageIndex"] = Constants.DefaultPage.ToString(),
            ["pageSize"] = Constants.DefaultSize.ToString(),
        };
        var data = await _rest.GetAsync(Constants.EpDataSecuritiesSummary, p, ct: ct);
        return SecuritiesSummary.FromJsonArray(Converter.GetProp(data, "data"));
    }

    public async Task<List<SecuritiesSummary>> GetSecuritiesSummaryByIndexHistoricalAsync(string index, string fromDate, string toDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(index, "index");
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        var p = new Dictionary<string, string>
        {
            ["index"] = index,
            ["from"] = fromDate,
            ["to"] = toDate,
            ["pageIndex"] = Constants.DefaultPage.ToString(),
            ["pageSize"] = Constants.DefaultSize.ToString(),
        };
        var data = await _rest.GetAsync(Constants.EpDataSecuritiesSummary, p, ct: ct);
        return SecuritiesSummary.FromJsonArray(Converter.GetProp(data, "data"));
    }
}

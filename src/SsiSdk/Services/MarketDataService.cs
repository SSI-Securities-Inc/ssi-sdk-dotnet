using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using SsiSdk.Internal;
using SsiSdk.Models;
using SsiSdk.Transport;

namespace SsiSdk.Services;

public sealed partial class MarketDataService
{
    private static readonly string[] RestBoards = [Board.HOSE, Board.HNX, Board.UPCOM];

    private const string WeekMonthRejected =
        "The server does not support the {0} timeframe (error 400210); " +
        "request 1d bars and aggregate them client-side instead";

    [GeneratedRegex(@"\A\d{4}[/-]\d{2}[/-]\d{2}\z")]
    private static partial Regex DateOnlyRegex();

    private readonly RestClient _rest;

    internal MarketDataService(RestClient rest) => _rest = rest;

    // -- shared helpers (internal for tests) -------------------------------

    /// <summary>Rows of a list endpoint. A bare array, the 204 marker (no rows) and <c>{"data": [...]}</c> are accepted.</summary>
    internal static List<JsonElement> Rows(JsonElement data, string what)
    {
        if (data.ValueKind == JsonValueKind.Array) return Converter.GetArray(data);
        if (Converter.IsNoContent(data)) return [];
        if (data.ValueKind == JsonValueKind.Object)
        {
            if (!data.TryGetProperty("data", out var rows) || rows.ValueKind == JsonValueKind.Null) return [];
            if (rows.ValueKind == JsonValueKind.Array) return Converter.GetArray(rows);
        }
        throw new ApiException($"Unexpected response format while reading {what}");
    }

    internal static int PagesCount(JsonElement data) =>
        data.ValueKind == JsonValueKind.Object
            ? Converter.ToOptInt(Converter.Pick(data, "pagesCount", "pageCount")) ?? 1
            : 1;

    internal static string? RestBoard(string? board)
    {
        if (board is null) return null;
        foreach (var b in RestBoards)
        {
            if (string.Equals(b, board.Trim(), StringComparison.OrdinalIgnoreCase)) return b;
        }
        throw new ValidationException($"board must be one of {string.Join(", ", RestBoards)}, got '{board}'");
    }

    internal static string CheckTimeframe(string timeframe)
    {
        if (!Timeframe.Allowed.Contains(timeframe))
        {
            var allowed = string.Join(", ", Timeframe.Allowed.OrderBy(t => t, StringComparer.Ordinal));
            throw new ValidationException($"timeframe {timeframe} is not supported; use one of {allowed}");
        }
        return timeframe;
    }

    internal static void CheckPaging(int page, int size)
    {
        Validate.RequirePositive(page, "page");
        Validate.RequirePositive(size, "size");
    }

    internal static string WithTime(string value, string timeText)
    {
        var text = value.Trim();
        return DateOnlyRegex().IsMatch(text) ? $"{text.Replace('-', '/')} {timeText}" : value;
    }

    internal static string ClampFutureEnd(string timeframe, string fromDate, string toDate, DateTime? now = null)
    {
        if (timeframe == Timeframe.Day1) return toDate;
        var today = (now ?? DateTime.Now).Date;
        var end = Validate.ParseDateArg(toDate, "to", allowTime: true).Date;
        var start = Validate.ParseDateArg(fromDate, "from", allowTime: true).Date;
        return end > today && today >= start ? today.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) + " 23:59:59" : toDate;
    }

    internal static List<KeyValuePair<string, string>> BuildOhlcParams(
        string symbol, string timeframe, string? fromDate, string? toDate, int page, int size)
    {
        Validate.RequireSymbol(symbol, "symbol");
        CheckTimeframe(timeframe);
        CheckPaging(page, size);
        var from = string.IsNullOrEmpty(fromDate) ? IdGenerator.BeginningOfDay() : fromDate;
        var to = string.IsNullOrEmpty(toDate) ? IdGenerator.EndOfDay() : toDate;
        Validate.RequireDateRange(from, to, allowTime: true);
        from = WithTime(from, "00:00:00");
        to = WithTime(to, "23:59:59");
        to = ClampFutureEnd(timeframe, from, to);
        return
        [
            new("symbol", symbol),
            new("from", from),
            new("to", to),
            new("timeFrame", timeframe),
            new("pageIndex", page.ToString(CultureInfo.InvariantCulture)),
            new("pageSize", size.ToString(CultureInfo.InvariantCulture)),
        ];
    }

    private static List<KeyValuePair<string, string>> BuildSecuritiesSummaryParams(
        string fromDate, string toDate, string? symbol, string? index, int page, int size)
    {
        Validate.RequireExactlyOne(symbol is not null, index is not null, "symbol", "index");
        Validate.RequireDateRange(fromDate, toDate);
        CheckPaging(page, size);
        var p = new List<KeyValuePair<string, string>>
        {
            new("from", fromDate),
            new("to", toDate),
            new("pageIndex", page.ToString(CultureInfo.InvariantCulture)),
            new("pageSize", size.ToString(CultureInfo.InvariantCulture)),
        };
        if (symbol is not null) p.Add(new("symbol", symbol));
        if (index is not null) p.Add(new("index", index));
        return p;
    }

    private static List<KeyValuePair<string, string>> BuildMasterDataParams(string fromDate, string toDate, int page, int size)
    {
        Validate.RequireDateRange(fromDate, toDate);
        CheckPaging(page, size);
        return
        [
            new("from", fromDate),
            new("to", toDate),
            new("pageIndex", page.ToString(CultureInfo.InvariantCulture)),
            new("pageSize", size.ToString(CultureInfo.InvariantCulture)),
        ];
    }

    // -- OHLC --------------------------------------------------------------

    private static Task<List<OhlcData>> NotImplementedDownload() =>
        Task.FromException<List<OhlcData>>(new NotImplementedException("OHLC download is not implemented yet"));

    private async Task<List<OhlcData>> GetOhlcAsync(
        string symbol, string timeframe, string? fromDate, string? toDate,
        int page, int size, CancellationToken ct)
    {
        var p = BuildOhlcParams(symbol, timeframe, fromDate, toDate, page, size);
        var data = await _rest.GetAsync(Constants.EpDataOhlc, p, ct: ct).ConfigureAwait(false);
        return OhlcData.FromRows(Rows(data, "OHLC"));
    }

    private Task<List<OhlcData>> GetOhlcHistoricalAsync(
        string symbol, string timeframe, string fromDate, string toDate, int page, int size, CancellationToken ct)
    {
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        return GetOhlcAsync(symbol, timeframe, fromDate, toDate, page, size, ct);
    }

    /// <summary>Every bar in a range, page after page until a short page. Order is the server's (newest first).</summary>
    public async IAsyncEnumerable<OhlcData> IterOhlcAsync(
        string symbol, string timeframe, string? fromDate = null, string? toDate = null,
        int size = Constants.DefaultSize, [EnumeratorCancellation] CancellationToken ct = default)
    {
        Validate.RequirePositive(size, "size");
        var page = Constants.DefaultPage;
        while (true)
        {
            var bars = await GetOhlcAsync(symbol, timeframe, fromDate, toDate, page, size, ct).ConfigureAwait(false);
            foreach (var bar in bars) yield return bar;
            if (bars.Count < size) yield break;
            page++;
        }
    }

    /// <summary>Every bar in a range across all pages. Newest first, or oldest first when <paramref name="ascending"/>.</summary>
    public async Task<List<OhlcData>> GetOhlcAllAsync(
        string symbol, string timeframe, string? fromDate = null, string? toDate = null,
        int size = Constants.DefaultSize, bool ascending = false, CancellationToken ct = default)
    {
        var bars = new List<OhlcData>();
        await foreach (var bar in IterOhlcAsync(symbol, timeframe, fromDate, toDate, size, ct).ConfigureAwait(false))
            bars.Add(bar);
        if (ascending) bars.Reverse();
        return bars;
    }

    public Task<List<OhlcData>> DownloadOhlc1MinuteAsync(string symbol, CancellationToken ct = default) => NotImplementedDownload();

    public Task<List<OhlcData>> DownloadOhlc1DayAsync(string symbol, CancellationToken ct = default) => NotImplementedDownload();

    public Task<List<OhlcData>> GetOhlc1MinuteAsync(string symbol, CancellationToken ct = default) =>
        GetOhlcAsync(symbol, Timeframe.Minute1, null, null, Constants.DefaultPage, Constants.DefaultSize, ct);

    public Task<List<OhlcData>> GetOhlc1MinuteHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default) =>
        GetOhlcHistoricalAsync(symbol, Timeframe.Minute1, fromDate, toDate, page, size, ct);

    public Task<List<OhlcData>> GetOhlc3MinuteAsync(string symbol, CancellationToken ct = default) =>
        GetOhlcAsync(symbol, Timeframe.Minute3, null, null, Constants.DefaultPage, Constants.DefaultSize, ct);

    public Task<List<OhlcData>> GetOhlc3MinuteHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default) =>
        GetOhlcHistoricalAsync(symbol, Timeframe.Minute3, fromDate, toDate, page, size, ct);

    public Task<List<OhlcData>> GetOhlc5MinuteAsync(string symbol, CancellationToken ct = default) =>
        GetOhlcAsync(symbol, Timeframe.Minute5, null, null, Constants.DefaultPage, Constants.DefaultSize, ct);

    public Task<List<OhlcData>> GetOhlc5MinuteHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default) =>
        GetOhlcHistoricalAsync(symbol, Timeframe.Minute5, fromDate, toDate, page, size, ct);

    public Task<List<OhlcData>> GetOhlc15MinuteAsync(string symbol, CancellationToken ct = default) =>
        GetOhlcAsync(symbol, Timeframe.Minute15, null, null, Constants.DefaultPage, Constants.DefaultSize, ct);

    public Task<List<OhlcData>> GetOhlc15MinuteHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default) =>
        GetOhlcHistoricalAsync(symbol, Timeframe.Minute15, fromDate, toDate, page, size, ct);

    public Task<List<OhlcData>> GetOhlc1HourAsync(string symbol, CancellationToken ct = default) =>
        GetOhlcAsync(symbol, Timeframe.Hour1, null, null, Constants.DefaultPage, Constants.DefaultSize, ct);

    public Task<List<OhlcData>> GetOhlc1HourHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default) =>
        GetOhlcHistoricalAsync(symbol, Timeframe.Hour1, fromDate, toDate, page, size, ct);

    public Task<List<OhlcData>> GetOhlc1DayHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default) =>
        GetOhlcHistoricalAsync(symbol, Timeframe.Day1, fromDate, toDate, page, size, ct);

    /// <summary>The server does not serve 1w bars: always a ValidationException (request 1d bars and aggregate).</summary>
    [Obsolete("The server does not serve 1w bars (error 400210); request 1d bars and aggregate them client-side.")]
    public Task<List<OhlcData>> GetOhlc1WeekHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        throw new ValidationException(string.Format(CultureInfo.InvariantCulture, WeekMonthRejected, "1w"));
    }

    /// <summary>The server does not serve 1M bars: always a ValidationException (request 1d bars and aggregate).</summary>
    [Obsolete("The server does not serve 1M bars (error 400210); request 1d bars and aggregate them client-side.")]
    public Task<List<OhlcData>> GetOhlc1MonthHistoricalAsync(string symbol, string fromDate, string toDate, int page = Constants.DefaultPage, int size = Constants.DefaultSize, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        throw new ValidationException(string.Format(CultureInfo.InvariantCulture, WeekMonthRejected, "1M"));
    }

    // -- Index ---------------------------------------------------------------

    private async Task<List<MarketIndexes>> GetIndexesCoreAsync(string? board, CancellationToken ct)
    {
        var p = new List<KeyValuePair<string, string>>();
        if (RestBoard(board) is { } b) p.Add(new("board", b));
        var data = await _rest.GetAsync(Constants.EpDataIndexList, p, ct: ct).ConfigureAwait(false);
        return MarketIndexes.FromRows(Rows(data, "indexes"));
    }

    public Task<List<MarketIndexes>> GetIndexesAsync(CancellationToken ct = default) => GetIndexesCoreAsync(null, ct);

    public Task<List<MarketIndexes>> GetIndexesByBoardAsync(string board, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(board, "board");
        return GetIndexesCoreAsync(board, ct);
    }

    // -- Index summary ---------------------------------------------------------

    private async Task<MarketIndexSummary?> GetIndexSummaryCoreAsync(string? index, string? board, string? tradingDate, CancellationToken ct)
    {
        var p = new List<KeyValuePair<string, string>>();
        if (index is not null) p.Add(new("index", index));
        if (RestBoard(board) is { } b) p.Add(new("board", b));
        if (tradingDate is not null) p.Add(new("tradingDate", tradingDate));
        var data = await _rest.GetAsync(Constants.EpDataIndexSummary, p, ct: ct).ConfigureAwait(false);
        var list = MarketIndexSummary.FromRows(Rows(data, "index summary"));
        return list.Count > 0 ? list[0] : null;
    }

    public Task<MarketIndexSummary?> GetIndexSummaryAsync(string index, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(index, "index");
        return GetIndexSummaryCoreAsync(index, null, null, ct);
    }

    public Task<MarketIndexSummary?> GetIndexSummaryHistoricalAsync(string index, string tradingDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(index, "index");
        Validate.RequireNonEmpty(tradingDate, "tradingDate");
        return GetIndexSummaryCoreAsync(index, null, tradingDate, ct);
    }

    public Task<MarketIndexSummary?> GetBoardSummaryAsync(string board, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(board, "board");
        return GetIndexSummaryCoreAsync(null, board, null, ct);
    }

    public Task<MarketIndexSummary?> GetBoardSummaryHistoricalAsync(string board, string tradingDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(board, "board");
        Validate.RequireNonEmpty(tradingDate, "tradingDate");
        return GetIndexSummaryCoreAsync(null, board, tradingDate, ct);
    }

    // -- Securities info -------------------------------------------------------

    private async Task<List<SecuritiesInfo>> GetSecuritiesInfoCoreAsync(string? index, string? board, string? symbol, CancellationToken ct)
    {
        var p = new List<KeyValuePair<string, string>>();
        if (symbol is not null) p.Add(new("symbol", symbol));
        if (RestBoard(board) is { } b) p.Add(new("board", b));
        if (index is not null) p.Add(new("index", index));
        var data = await _rest.GetAsync(Constants.EpDataSecuritiesByBoard, p, ct: ct).ConfigureAwait(false);
        return SecuritiesInfo.FromRows(Rows(data, "securities info"));
    }

    public async Task<SecuritiesInfo?> GetSecuritiesInfoAsync(string symbol, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        var list = await GetSecuritiesInfoCoreAsync(null, null, symbol, ct).ConfigureAwait(false);
        return list.Count > 0 ? list[0] : null;
    }

    public Task<List<SecuritiesInfo>> GetSecuritiesInfoByIndexAsync(string index, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(index, "index");
        return GetSecuritiesInfoCoreAsync(index, null, null, ct);
    }

    public Task<List<SecuritiesInfo>> GetSecuritiesInfoByBoardAsync(string board, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(board, "board");
        return GetSecuritiesInfoCoreAsync(null, board, null, ct);
    }

    // -- Securities summary ----------------------------------------------------

    private async Task<List<SecuritiesSummary>> GetSecuritiesSummaryCoreAsync(
        string fromDate, string toDate, string? symbol, string? index, CancellationToken ct)
    {
        var page = Constants.DefaultPage;
        var size = Constants.DefaultSize;
        var p = BuildSecuritiesSummaryParams(fromDate, toDate, symbol, index, page, size);
        var data = await _rest.GetAsync(Constants.EpDataSecuritiesSummary, p, ct: ct).ConfigureAwait(false);
        var items = SecuritiesSummary.FromRows(Rows(data, "securities summary"));
        var pages = PagesCount(data);
        while (page < pages && items.Count > 0)
        {
            page++;
            p = BuildSecuritiesSummaryParams(fromDate, toDate, symbol, index, page, size);
            data = await _rest.GetAsync(Constants.EpDataSecuritiesSummary, p, ct: ct).ConfigureAwait(false);
            var more = SecuritiesSummary.FromRows(Rows(data, "securities summary"));
            if (more.Count == 0) break;
            items.AddRange(more);
        }
        return items;
    }

    public Task<List<SecuritiesSummary>> GetSecuritiesSummaryAsync(string symbol, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        var today = IdGenerator.TodayDateStr();
        return GetSecuritiesSummaryCoreAsync(today, today, symbol, null, ct);
    }

    public Task<List<SecuritiesSummary>> GetSecuritiesSummaryHistoricalAsync(string symbol, string fromDate, string toDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(symbol, "symbol");
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        return GetSecuritiesSummaryCoreAsync(fromDate, toDate, symbol, null, ct);
    }

    public Task<List<SecuritiesSummary>> GetSecuritiesSummaryByIndexAsync(string index, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(index, "index");
        var today = IdGenerator.TodayDateStr();
        return GetSecuritiesSummaryCoreAsync(today, today, null, index, ct);
    }

    public Task<List<SecuritiesSummary>> GetSecuritiesSummaryByIndexHistoricalAsync(string index, string fromDate, string toDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(index, "index");
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        return GetSecuritiesSummaryCoreAsync(fromDate, toDate, null, index, ct);
    }

    // -- Master data -------------------------------------------------------------

    private async Task<List<MasterData>> GetMasterDataCoreAsync(string fromDate, string toDate, CancellationToken ct)
    {
        var page = Constants.DefaultPage;
        var items = new List<MasterData>();
        while (true)
        {
            var p = BuildMasterDataParams(fromDate, toDate, page, Constants.DefaultSize);
            var data = await _rest.GetAsync(Constants.EpDataMasterData, p, ct: ct).ConfigureAwait(false);
            items.AddRange(MasterData.FromRows(Rows(data, "master data")));
            if (page >= PagesCount(data)) break;
            page++;
        }
        return items;
    }

    public Task<List<MasterData>> GetMasterDataAsync(CancellationToken ct = default)
    {
        var today = IdGenerator.TodayDateStr();
        return GetMasterDataCoreAsync(today, today, ct);
    }

    public Task<List<MasterData>> GetMasterDataHistoricalAsync(string fromDate, string toDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        return GetMasterDataCoreAsync(fromDate, toDate, ct);
    }
}

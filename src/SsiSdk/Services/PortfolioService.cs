using System.Text.Json;
using SsiSdk.Internal;
using SsiSdk.Models;
using SsiSdk.Transport;

namespace SsiSdk.Services;

public sealed class PortfolioService
{
    private readonly RestClient _rest;
    private readonly string _clientId;

    internal PortfolioService(RestClient rest, Config config)
    {
        _rest = rest;
        _clientId = config.ClientId;
    }

    public async Task<EquityAccountBalance?> GetEquityBalanceAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var data = await GetBalanceAsync(accountNo, ct);
        return PM.NonEmptyObject(data, "equity") is { } eq ? EquityAccountBalance.FromJson(eq) : null;
    }

    public async Task<DerivativeAccountBalance?> GetDerivativeBalanceAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var data = await GetBalanceAsync(accountNo, ct);
        return PM.NonEmptyObject(data, "derivative") is { } der ? DerivativeAccountBalance.FromJson(der) : null;
    }

    private async Task<JsonElement> GetBalanceAsync(string accountNo, CancellationToken ct) =>
        Converter.Payload(await _rest.GetAsync(Constants.EpAccountBalance, BalanceParams(accountNo), ct: ct));

    /// <summary>Equity positions, or null when the account has none.</summary>
    public async Task<List<EquityPosition>?> GetEquityPositionsAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        return (await GetPositionsAsync(accountNo, ct)).Equity;
    }

    public async Task<List<DerivativePosition>> GetOpenDerivativePositionsAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        return (await GetPositionsAsync(accountNo, ct)).Derivative?.OpenPositions ?? [];
    }

    public async Task<List<DerivativePosition>> GetClosedDerivativePositionsAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        return (await GetPositionsAsync(accountNo, ct)).Derivative?.ClosedPositions ?? [];
    }

    /// <summary>Open and closed derivative positions, or null when the account has none.</summary>
    public async Task<AllDerivativePosition?> GetDerivativePositionsAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        return (await GetPositionsAsync(accountNo, ct)).Derivative;
    }

    private async Task<Position> GetPositionsAsync(string accountNo, CancellationToken ct)
    {
        var data = await _rest.GetAsync(Constants.EpPositions, PositionsParams(accountNo), ct: ct);
        return Position.FromJson(Converter.Payload(data), accountNo);
    }

    public async Task<List<Order>> GetTodayOrdersAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var today = IdGenerator.TodayDateStr();
        return await GetOrdersAsync(accountNo, today, today, ct);
    }

    public async Task<List<Order>> GetHistoricalOrdersAsync(string accountNo, string fromDate, string toDate, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        Validate.RequireNonEmpty(fromDate, "fromDate");
        Validate.RequireNonEmpty(toDate, "toDate");
        return await GetOrdersAsync(accountNo, fromDate, toDate, ct);
    }

    private async Task<List<Order>> GetOrdersAsync(string accountNo, string fromDate, string toDate, CancellationToken ct)
    {
        var data = await _rest.GetAsync(Constants.EpOrderHistory, OrderBookParams(accountNo, fromDate, toDate), ct: ct);
        return OrderBook.FromJson(data).Orders;
    }

    public async Task<EquityPPMMR?> GetEquityPpmmrAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var data = await GetPpmmrAsync(accountNo, ct);
        return PM.NonEmptyObject(data, "equity") is { } eq ? EquityPPMMR.FromJson(eq) : null;
    }

    public async Task<DerivativePPMMR?> GetDerivativePpmmrAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var data = await GetPpmmrAsync(accountNo, ct);
        return PM.NonEmptyObject(data, "derivative") is { } der ? DerivativePPMMR.FromJson(der) : null;
    }

    private async Task<JsonElement> GetPpmmrAsync(string accountNo, CancellationToken ct) =>
        Converter.Payload(await _rest.GetAsync(Constants.EpAccountPpmmr, PpmmrParams(accountNo), ct: ct));

    private List<KeyValuePair<string, string>> BalanceParams(string accountNo) =>
        [new("clientId", _clientId), new("accountNo", accountNo)];

    private List<KeyValuePair<string, string>> PositionsParams(string accountNo) =>
        [new("clientId", _clientId), new("accountNo", accountNo), new("querySummary", "true")];

    internal static List<KeyValuePair<string, string>> OrderBookParams(string accountNo, string fromDate, string toDate) =>
    [
        new("accountNo", accountNo),
        new("from", fromDate),
        new("to", toDate),
        new("pageIndex", Constants.DefaultPage.ToString()),
        new("pageSize", Constants.DefaultSize.ToString()),
    ];

    private static List<KeyValuePair<string, string>> PpmmrParams(string accountNo) => [new("accountNo", accountNo)];
}

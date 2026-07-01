using System.Text.Json;
using SsiSdk.Internal;
using SsiSdk.Models;
using SsiSdk.Transport;

namespace SsiSdk.Services;

public sealed class PortfolioService
{
    private static readonly Logger Log = new("ssi_sdk.services.portfolio");
    private readonly RestClient _rest;
    private readonly string _clientId;

    internal PortfolioService(RestClient rest, string clientId)
    {
        _rest = rest;
        _clientId = clientId;
    }

    public async Task<EquityAccountBalance?> GetEquityBalanceAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var p = new Dictionary<string, string> { ["clientId"] = _clientId, ["accountNo"] = accountNo };
        var data = await _rest.GetAsync(Constants.EpAccountBalance, p, ct: ct);
        var eq = Converter.GetProp(data, "equity");
        return eq is not null ? EquityAccountBalance.FromJson(eq.Value) : null;
    }

    public async Task<DerivativeAccountBalance?> GetDerivativeBalanceAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var p = new Dictionary<string, string> { ["clientId"] = _clientId, ["accountNo"] = accountNo };
        var data = await _rest.GetAsync(Constants.EpAccountBalance, p, ct: ct);
        var der = Converter.GetProp(data, "derivative");
        return der is not null ? DerivativeAccountBalance.FromJson(der.Value) : null;
    }

    public async Task<List<EquityPosition>> GetEquityPositionsAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var p = new Dictionary<string, string> { ["clientId"] = _clientId, ["accountNo"] = accountNo };
        var data = await _rest.GetAsync(Constants.EpPositions, p, ct: ct);
        return EquityPosition.FromJsonArray(Converter.GetProp(data, "equity"));
    }

    public async Task<List<DerivativePosition>> GetOpenDerivativePositionsAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var data = await GetDerivativePositionsRaw(accountNo, ct);
        return data is not null
            ? DerivativePosition.FromJsonArray(Converter.GetProp(data.Value, "derOpenPositions"))
            : [];
    }

    public async Task<List<DerivativePosition>> GetClosedDerivativePositionsAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var data = await GetDerivativePositionsRaw(accountNo, ct);
        return data is not null
            ? DerivativePosition.FromJsonArray(Converter.GetProp(data.Value, "derClosePositions"))
            : [];
    }

    public async Task<AllDerivativePosition> GetDerivativePositionsAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var data = await GetDerivativePositionsRaw(accountNo, ct);
        return data is not null
            ? AllDerivativePosition.FromJson(data.Value)
            : new AllDerivativePosition();
    }

    private async Task<JsonElement?> GetDerivativePositionsRaw(string accountNo, CancellationToken ct)
    {
        var p = new Dictionary<string, string> { ["clientId"] = _clientId, ["accountNo"] = accountNo };
        var data = await _rest.GetAsync(Constants.EpPositions, p, ct: ct);
        return Converter.GetProp(data, "derivative");
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
        var p = new Dictionary<string, string>
        {
            ["accountNo"] = accountNo,
            ["from"] = fromDate,
            ["to"] = toDate,
            ["pageIndex"] = Constants.DefaultPage.ToString(),
            ["pageSize"] = Constants.DefaultSize.ToString(),
        };
        var data = await _rest.GetAsync(Constants.EpOrderHistory, p, ct: ct);
        var acct = Converter.ToStr(Converter.GetProp(data, "accountNo"));
        var orderList = Converter.GetProp(data, "orderList");
        if (orderList is null || orderList.Value.ValueKind != JsonValueKind.Array) return [];
        return Converter.GetArray(orderList).Select(el => Order.FromJson(el, acct)).ToList();
    }

    // PPMMR
    public async Task<EquityPPMMR?> GetEquityPpmmrAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var p = new Dictionary<string, string> { ["accountNo"] = accountNo };
        var data = await _rest.GetAsync(Constants.EpAccountPpmmr, p, ct: ct);
        var eq = Converter.GetProp(data, "equity");
        return eq is not null ? EquityPPMMR.FromJson(eq.Value) : null;
    }

    public async Task<DerivativePPMMR?> GetDerivativePpmmrAsync(string accountNo, CancellationToken ct = default)
    {
        Validate.RequireNonEmpty(accountNo, "accountNo");
        var p = new Dictionary<string, string> { ["accountNo"] = accountNo };
        var data = await _rest.GetAsync(Constants.EpAccountPpmmr, p, ct: ct);
        var der = Converter.GetProp(data, "derivative");
        return der is not null ? DerivativePPMMR.FromJson(der.Value) : null;
    }
}

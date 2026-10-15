using System.Text.Json;
using SsiSdk.Internal;
using SsiSdk.Models;
using SsiSdk.Transport;

namespace SsiSdk.Services;

public sealed class AccountService
{
    private readonly RestClient _rest;

    internal AccountService(RestClient rest) => _rest = rest;

    public async Task<List<Account>> GetAccountInfoAsync(CancellationToken ct = default)
    {
        var data = await _rest.GetAsync(Constants.EpAccountInfo, ct: ct);
        return ParseAccounts(data);
    }

    internal static List<Account> ParseAccounts(JsonElement data)
    {
        if (data.ValueKind == JsonValueKind.Array)
            return Account.FromRows(Converter.GetArray(data));
        if (Converter.IsNoContent(data))
            return [];
        throw new ApiException("Unexpected response format while listing accounts");
    }
}

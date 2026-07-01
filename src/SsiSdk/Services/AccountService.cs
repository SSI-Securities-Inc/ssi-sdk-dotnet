using System.Text.Json;
using SsiSdk.Internal;
using SsiSdk.Models;
using SsiSdk.Transport;

namespace SsiSdk.Services;

public sealed class AccountService
{
    private static readonly Logger Log = new("ssi_sdk.services.account");
    private readonly RestClient _rest;

    internal AccountService(RestClient rest) => _rest = rest;

    public async Task<List<Account>> GetAccountInfoAsync(CancellationToken ct = default)
    {
        var data = await _rest.GetAsync(Constants.EpAccountInfo, ct: ct);
        var arr = Converter.GetProp(data, "data");
        if (arr is null || arr.Value.ValueKind != JsonValueKind.Array) return [];

        return arr.Value.Deserialize<List<Account>>(Converter.CamelCase) ?? [];
    }
}

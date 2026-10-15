using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Models;

public sealed class Account
{
    public string AccountNo { get; set; } = string.Empty;

    /// <summary>An <see cref="SsiSdk.AccountType"/> value, the raw text for an unknown type (for example "2", or "" when the server sent ""), null when absent.</summary>
    public string? AccountType { get; set; }

    private static string? AccountTypeOf(JsonElement? raw)
    {
        if (raw is null or { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }) return null;
        var text = Converter.ToStr(raw);
        return SsiSdk.AccountType.FromValue(text) ?? text;
    }

    internal static List<Account> FromRows(IEnumerable<JsonElement> rows) =>
        rows.Where(r => r.ValueKind == JsonValueKind.Object)
            .Select(r => new Account
            {
                AccountNo = Converter.ToStr(Converter.GetProp(r, "accountNo")),
                AccountType = AccountTypeOf(Converter.GetProp(r, "accountType")),
            })
            .ToList();
}

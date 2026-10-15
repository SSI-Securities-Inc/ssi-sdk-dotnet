using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Models;

internal static class PM
{
    /// <summary>The first of the named fields that is present, as a decimal; absent or blank gives 0.</summary>
    public static decimal D(JsonElement el, params string[] names) => Converter.ToDecimal(Converter.Pick(el, names));

    public static int I(JsonElement el, params string[] names) => Converter.ToInt(Converter.Pick(el, names));

    public static string S(JsonElement el, string name) => Converter.ToStr(Converter.GetProp(el, name));

    /// <summary>A named object field that is present and not empty, otherwise null.</summary>
    public static JsonElement? NonEmptyObject(JsonElement el, string name)
    {
        var p = Converter.GetProp(el, name);
        return p is { ValueKind: JsonValueKind.Object } o && o.EnumerateObject().Any() ? o : null;
    }

    public static string? Known(string? raw, params string[] known)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        foreach (var k in known)
        {
            if (string.Equals(k, raw, StringComparison.OrdinalIgnoreCase)) return k;
        }
        return raw;
    }
}

public sealed class EquityAccountBalance
{
    public string AccountNo { get; set; } = string.Empty;
    public decimal AccountBalance { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal InterestLoan { get; set; }
    public decimal OverdueFeeLoan { get; set; }
    public decimal Withdrawable { get; set; }
    public decimal OnHoldCash { get; set; }
    public decimal SellUnmatched { get; set; }
    public decimal SellT0 { get; set; }
    public decimal SellT1 { get; set; }
    public decimal SellT2 { get; set; }
    public decimal BuyUnmatched { get; set; }
    public decimal BuyT0 { get; set; }
    public decimal BuyT1 { get; set; }
    public decimal BuyT2 { get; set; }
    public decimal AdvanceCashT0 { get; set; }
    public decimal AdvanceCashT1 { get; set; }
    public decimal HoldSubscription { get; set; }
    public decimal Dividend { get; set; }

    internal static EquityAccountBalance FromJson(JsonElement el)
    {
        return new EquityAccountBalance
        {
            AccountNo = Converter.ToStr(Converter.GetProp(el, "accountNo")),
            AccountBalance = PM.D(el, "accountBalance"),
            TotalDebt = PM.D(el, "totalDebt"),
            InterestLoan = PM.D(el, "interestLoan"),
            OverdueFeeLoan = PM.D(el, "overdueFeeLoan"),
            Withdrawable = PM.D(el, "withdrawable"),
            OnHoldCash = PM.D(el, "onHoldCash"),
            SellUnmatched = PM.D(el, "sellUnmatched"),
            SellT0 = PM.D(el, "sellT0"),
            SellT1 = PM.D(el, "sellT1"),
            SellT2 = PM.D(el, "sellT2"),
            BuyUnmatched = PM.D(el, "buyUnmatched"),
            BuyT0 = PM.D(el, "buyT0"),
            BuyT1 = PM.D(el, "buyT1"),
            BuyT2 = PM.D(el, "buyT2"),
            AdvanceCashT0 = PM.D(el, "advancedCashT0", "advanceCashT0"),
            AdvanceCashT1 = PM.D(el, "advancedCashT1", "advanceCashT1"),
            HoldSubscription = PM.D(el, "holdSubscription"),
            Dividend = PM.D(el, "dividend"),
        };
    }
}


public sealed class DerivativeAccountBalance
{
    public string AccountNo { get; set; } = string.Empty;
    public decimal AccountBalance { get; set; }
    public decimal Fee { get; set; }
    public decimal Commission { get; set; }
    public decimal Interest { get; set; }
    public decimal ExtInterest { get; set; }
    public decimal Loan { get; set; }
    public decimal DeliveryAmount { get; set; }
    public decimal FloatingPL { get; set; }
    public decimal TradingPL { get; set; }
    public decimal TotalPL { get; set; }
    public decimal Withdrawable { get; set; }
    public decimal CashSSI { get; set; }
    public decimal ValidNonCashSSI { get; set; }
    public decimal WithdrawableSSI { get; set; }
    public decimal CashVSDC { get; set; }
    public decimal ValidNonCashVSDC { get; set; }
    public decimal WithdrawableVSDC { get; set; }

    [Obsolete("Use WithdrawableSSI")]
    public decimal CashWithdrawableSSI => WithdrawableSSI;

    [Obsolete("Use WithdrawableVSDC")]
    public decimal CashWithdrawableVSDC => WithdrawableVSDC;

    internal static DerivativeAccountBalance FromJson(JsonElement el) => new()
    {
        AccountNo = Converter.ToStr(Converter.GetProp(el, "accountNo")),
        AccountBalance = PM.D(el, "accountBalance"),
        Fee = PM.D(el, "fee"),
        Commission = PM.D(el, "commission"),
        Interest = PM.D(el, "interest"),
        ExtInterest = PM.D(el, "extInterest"),
        Loan = PM.D(el, "loan"),
        DeliveryAmount = PM.D(el, "deliveryAmount"),
        FloatingPL = PM.D(el, "floatingPL"),
        TradingPL = PM.D(el, "tradingPL"),
        TotalPL = PM.D(el, "totalPL"),
        Withdrawable = PM.D(el, "withdrawable"),
        CashSSI = PM.D(el, "cashSSI"),
        ValidNonCashSSI = PM.D(el, "validNonCashSSI"),
        WithdrawableSSI = PM.D(el, "withdrawableSSI", "cashWithdrawableSSI"),
        CashVSDC = PM.D(el, "cashVSDC"),
        ValidNonCashVSDC = PM.D(el, "validNonCashVSDC"),
        WithdrawableVSDC = PM.D(el, "withdrawableVSDC", "cashWithdrawableVSDC"),
    };
}

public sealed class EquityPosition
{
    public string AccountNo { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int BlockQuantity { get; set; }
    public int DividendQuantity { get; set; }
    public int BuyingQuantity { get; set; }
    public int BoughtQuantity { get; set; }
    public int SellingQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public int T1SellQuantity { get; set; }
    public int T2SellQuantity { get; set; }
    public int T1BuyQuantity { get; set; }
    public int T2BuyQuantity { get; set; }
    public decimal CostPrice { get; set; }
    public int MortgageQuantity { get; set; }
    public int SellableQuantity { get; set; }
    public int RestrictedQuantity { get; set; }

    internal static EquityPosition FromJson(JsonElement el) => new()
    {
        AccountNo = Converter.ToStr(Converter.GetProp(el, "accountNo")),
        Symbol = Converter.ToStr(Converter.GetProp(el, "symbol")),
        Quantity = Converter.ToInt(Converter.GetProp(el, "quantity")),
        BlockQuantity = Converter.ToInt(Converter.GetProp(el, "blockQuantity")),
        DividendQuantity = Converter.ToInt(Converter.GetProp(el, "dividendQuantity")),
        BuyingQuantity = Converter.ToInt(Converter.GetProp(el, "buyingQuantity")),
        BoughtQuantity = Converter.ToInt(Converter.GetProp(el, "boughtQuantity")),
        SellingQuantity = Converter.ToInt(Converter.GetProp(el, "sellingQuantity")),
        SoldQuantity = Converter.ToInt(Converter.GetProp(el, "soldQuantity")),
        T1SellQuantity = Converter.ToInt(Converter.GetProp(el, "t1SellQuantity")),
        T2SellQuantity = Converter.ToInt(Converter.GetProp(el, "t2SellQuantity")),
        T1BuyQuantity = Converter.ToInt(Converter.GetProp(el, "t1BuyQuantity")),
        T2BuyQuantity = Converter.ToInt(Converter.GetProp(el, "t2BuyQuantity")),
        CostPrice = PM.D(el, "costPrice"),
        MortgageQuantity = Converter.ToInt(Converter.GetProp(el, "mortgageQuantity")),
        SellableQuantity = Converter.ToInt(Converter.GetProp(el, "sellableQuantity")),
        RestrictedQuantity = Converter.ToInt(Converter.GetProp(el, "restrictedQuantity")),
    };

    internal static List<EquityPosition> FromJsonArray(JsonElement? el) =>
        Converter.GetArray(el).Select(FromJson).ToList();
}

public sealed class DerivativePosition
{
    public string AccountNo { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int Long { get; set; }
    public int Short { get; set; }
    public int Net { get; set; }
    public decimal BidAvgPrice { get; set; }
    public decimal AskAvgPrice { get; set; }
    public decimal TradePrice { get; set; }
    public decimal FloatingPL { get; set; }
    public decimal TradingPL { get; set; }

    internal static DerivativePosition FromJson(JsonElement el, string accountNo = "") => new()
    {
        AccountNo = Converter.ToOptStr(Converter.GetProp(el, "accountNo")) ?? accountNo,
        Symbol = Converter.ToStr(Converter.GetProp(el, "symbol")),
        Long = Converter.ToInt(Converter.GetProp(el, "long")),
        Short = Converter.ToInt(Converter.GetProp(el, "short")),
        Net = Converter.ToInt(Converter.GetProp(el, "net")),
        BidAvgPrice = PM.D(el, "bidAvgPrice"),
        AskAvgPrice = PM.D(el, "askAvgPrice"),
        TradePrice = PM.D(el, "tradePrice"),
        FloatingPL = PM.D(el, "floatingPL"),
        TradingPL = PM.D(el, "tradingPL"),
    };

    internal static List<DerivativePosition> FromJsonArray(JsonElement? el, string accountNo = "") =>
        Converter.GetArray(el).Select(i => FromJson(i, accountNo)).ToList();
}

public sealed class AllDerivativePosition
{
    public List<DerivativePosition> OpenPositions { get; set; } = [];
    public List<DerivativePosition> ClosedPositions { get; set; } = [];
    public string AccountNo { get; set; } = string.Empty;

    /// <summary>A bare list counts as the open positions; anything else that is not an object is empty.</summary>
    internal static AllDerivativePosition FromJson(JsonElement el, string accountNo = "")
    {
        if (el.ValueKind == JsonValueKind.Array)
            return new AllDerivativePosition { OpenPositions = DerivativePosition.FromJsonArray(el, accountNo), AccountNo = accountNo };
        if (el.ValueKind != JsonValueKind.Object)
            return new AllDerivativePosition { AccountNo = accountNo };
        return new AllDerivativePosition
        {
            OpenPositions = DerivativePosition.FromJsonArray(Converter.GetProp(el, "derOpenPositions"), accountNo),
            ClosedPositions = DerivativePosition.FromJsonArray(Converter.GetProp(el, "derClosePositions"), accountNo),
            AccountNo = accountNo,
        };
    }
}

/// <summary>Positions of an account; a part the server did not return is null.</summary>
public sealed class Position
{
    public List<EquityPosition>? Equity { get; set; }
    public AllDerivativePosition? Derivative { get; set; }

    internal static Position FromJson(JsonElement data, string accountNo = "")
    {
        if (Converter.IsNoContent(data) || data.ValueKind != JsonValueKind.Object) return new Position();
        var equity = Converter.GetProp(data, "equity");
        var derivative = Converter.GetProp(data, "derivative");
        return new Position
        {
            Equity = equity is { ValueKind: JsonValueKind.Array } eq && eq.GetArrayLength() > 0
                ? EquityPosition.FromJsonArray(eq)
                : null,
            Derivative = derivative is { } der && IsNonEmpty(der) ? AllDerivativePosition.FromJson(der, accountNo) : null,
        };
    }

    private static bool IsNonEmpty(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Object => el.EnumerateObject().Any(),
        JsonValueKind.Array => el.GetArrayLength() > 0,
        _ => false,
    };
}

public sealed class Order
{
    public string AccountNo { get; set; } = string.Empty;
    public string ClientRequestId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;

    /// <summary>"B" / "S", the raw text for an unknown value, null when absent.</summary>
    public string? Side { get; set; }

    /// <summary>An order type such as "LO" or "ATO", the raw text for an unknown value, null when absent.</summary>
    public string? OrderType { get; set; }

    /// <summary>Limit price; 0 when the order has no numeric price (see <see cref="PriceMarker"/>).</summary>
    public decimal Price { get; set; }

    /// <summary>The price word the server sent instead of a number (for example "ATO" or "MP"); null for a numeric price.</summary>
    public string? PriceMarker { get; set; }
    public decimal AvgPrice { get; set; }
    public int Quantity { get; set; }

    /// <summary>Outstanding quantity; null when the server did not send it.</summary>
    public int? OsQuantity { get; set; }
    public int FilledQuantity { get; set; }
    public int CancelQuantity { get; set; }

    /// <summary>An <see cref="OrderStatus"/> code, the raw text for an unknown one, null when absent.</summary>
    public string? Status { get; set; }
    public string InputTime { get; set; } = string.Empty;
    public string ModifyTime { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    private static readonly string[] OrderTypes =
        [SsiSdk.OrderType.ATO, SsiSdk.OrderType.ATC, SsiSdk.OrderType.LO, SsiSdk.OrderType.MTL, SsiSdk.OrderType.MP,
         SsiSdk.OrderType.MOK, SsiSdk.OrderType.MAK, SsiSdk.OrderType.PLO];

    private static readonly string[] Statuses =
    [
        SsiSdk.OrderStatus.Pending, SsiSdk.OrderStatus.PendingApproval, SsiSdk.OrderStatus.Ready, SsiSdk.OrderStatus.Sent,
        SsiSdk.OrderStatus.Queued, SsiSdk.OrderStatus.Filled, SsiSdk.OrderStatus.PartialFilled,
        SsiSdk.OrderStatus.PartialCancelled, SsiSdk.OrderStatus.PendingModify, SsiSdk.OrderStatus.PendingCancel,
        SsiSdk.OrderStatus.Cancelled, SsiSdk.OrderStatus.Rejected, SsiSdk.OrderStatus.Expired, SsiSdk.OrderStatus.PreSession,
        SsiSdk.OrderStatus.Soi, SsiSdk.OrderStatus.Pas, SsiSdk.OrderStatus.Cpl, SsiSdk.OrderStatus.Requested,
        SsiSdk.OrderStatus.Error,
    ];

    internal static string? NormalizeOrderType(string? raw) => PM.Known(raw, OrderTypes);

    internal static string? NormalizeStatus(string? raw) => PM.Known(raw, Statuses);

    /// <summary>Number gives (value, null); blank gives (0, null); a word gives (0, word).</summary>
    internal static (decimal Price, string? Marker) ParsePrice(JsonElement? el)
    {
        if (Converter.ToDecimalOrNull(el) is { } n) return (n, null);
        var text = Converter.ToOptStr(el)?.Trim();
        return string.IsNullOrEmpty(text) ? (0m, null) : (0m, NormalizeOrderType(text));
    }

    internal static Order FromJson(JsonElement el, string accountNo)
    {
        var (price, marker) = ParsePrice(Converter.GetProp(el, "price"));
        return new Order
        {
            AccountNo = accountNo,
            ClientRequestId = PM.S(el, "clientRequestId"),
            OrderId = PM.S(el, "orderId"),
            Symbol = Converter.ToStr(Converter.Pick(el, "symbol", "instrumentId")),
            Side = PM.Known(Converter.ToOptStr(Converter.Pick(el, "side", "buySell")), SsiSdk.OrderSide.Buy, SsiSdk.OrderSide.Sell),
            OrderType = NormalizeOrderType(Converter.ToOptStr(Converter.GetProp(el, "orderType"))),
            Price = price,
            PriceMarker = marker,
            AvgPrice = PM.D(el, "avgPrice"),
            Quantity = PM.I(el, "quantity"),
            OsQuantity = Converter.ToOptInt(Converter.GetProp(el, "osQuantity")),
            FilledQuantity = PM.I(el, "filledQty", "filledQuantity"),
            CancelQuantity = PM.I(el, "cancelQty", "cancelQuantity"),
            Status = NormalizeStatus(Converter.ToOptStr(Converter.GetProp(el, "orderStatus"))),
            InputTime = PM.S(el, "inputTime"),
            ModifyTime = PM.S(el, "modifiedTime"),
            Message = PM.S(el, "message"),
        };
    }
}

/// <summary>Orders of one account and the server's total record count.</summary>
public sealed class OrderBook
{
    public List<Order> Orders { get; set; } = [];
    public int TotalOrders { get; set; }

    internal static OrderBook FromJson(JsonElement data)
    {
        data = Converter.Payload(data);
        if (Converter.IsNoContent(data) || data.ValueKind != JsonValueKind.Object) return new OrderBook();
        var accountNo = PM.S(data, "accountNo");
        return new OrderBook
        {
            Orders = Converter.GetArray(Converter.GetProp(data, "orderList"))
                .Where(o => o.ValueKind == JsonValueKind.Object)
                .Select(o => Order.FromJson(o, accountNo))
                .ToList(),
            TotalOrders = PM.I(data, "totalRecord"),
        };
    }
}

public sealed class EquityPPMMR
{
    public string AccountNo { get; set; } = string.Empty;
    public decimal Dividend { get; set; }
    public decimal LoanValue { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal Debt { get; set; }
    public decimal Liability { get; set; }
    public decimal LiabilitySSI { get; set; }
    public decimal NetLiability { get; set; }
    public decimal Fees { get; set; }
    public decimal InterestSSI { get; set; }
    public decimal InterestSPV { get; set; }
    public decimal Withdrawable { get; set; }
    public decimal EE { get; set; }
    public decimal EE50 { get; set; }
    public decimal EE60 { get; set; }
    public decimal EE70 { get; set; }
    public decimal EE80 { get; set; }
    public decimal EE90 { get; set; }
    public decimal Action { get; set; }
    public decimal ActionSSI { get; set; }
    public decimal Equity { get; set; }
    public decimal EquitySSI { get; set; }
    public decimal EECash { get; set; }
    public decimal HoldSubscription { get; set; }
    public decimal BankBalance { get; set; }
    public decimal OnHoldCash { get; set; }
    public decimal Doverdue { get; set; }
    public decimal DoverdueSSI { get; set; }
    public decimal AccountBalance { get; set; }
    public decimal D { get; set; }
    public decimal DSpv { get; set; }
    public decimal DSsi { get; set; }
    public decimal Cia { get; set; }
    public decimal CollateralAsset { get; set; }
    public decimal CollateralAssetSSI { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal TotalEquitySSI { get; set; }
    public decimal Lmv { get; set; }
    public decimal LmvMargin { get; set; }
    public decimal LmvMarginSSI { get; set; }
    public decimal CallLmv { get; set; }
    public decimal ForceLmv { get; set; }
    public decimal CallLmvSSI { get; set; }
    public decimal ForceLmvSSI { get; set; }
    public decimal LmvNonMarginable { get; set; }
    public decimal LmvNonMarginableSSI { get; set; }
    public decimal PreLoan { get; set; }
    public decimal MarginRatio { get; set; }
    public decimal MarginRatioSSI { get; set; }
    public decimal PurchasingPower { get; set; }
    public decimal EeOrigin { get; set; }
    public decimal BuyUnmatched { get; set; }
    public decimal SellUnmatched { get; set; }
    public decimal BuyT0 { get; set; }
    public decimal BuyT1 { get; set; }
    public decimal BuyT2 { get; set; }
    public decimal SellT0 { get; set; }
    public decimal SellT1 { get; set; }
    public decimal SellT2 { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal MarginCallLmvSold { get; set; }
    public decimal MarginCallLmvSoldSSI { get; set; }
    public decimal MarginCall { get; set; }
    public decimal MarginCallSSI { get; set; }
    public decimal CollateralA { get; set; }
    public decimal CollateralNon { get; set; }
    public decimal CollateralASSI { get; set; }
    public decimal CollateralNonSSI { get; set; }
    public decimal CallMargin { get; set; }
    public decimal CallForceSell { get; set; }
    public decimal CallMarginSSI { get; set; }
    public decimal CallForceSellSSI { get; set; }
    public decimal Ar { get; set; }

    internal static EquityPPMMR FromJson(JsonElement el) => new()
    {
        AccountNo = Converter.ToStr(Converter.GetProp(el, "accountNo")),
        Dividend = PM.D(el, "dividend"),
        LoanValue = PM.D(el, "loanValue"),
        TotalDebt = PM.D(el, "totalDebt"),
        Debt = PM.D(el, "debt"),
        Liability = PM.D(el, "liability"),
        LiabilitySSI = PM.D(el, "liabilitySSI"),
        NetLiability = PM.D(el, "netLiability"),
        Fees = PM.D(el, "fees"),
        InterestSSI = PM.D(el, "interestSSI"),
        InterestSPV = PM.D(el, "interestSPV"),
        Withdrawable = PM.D(el, "withdrawable"),
        EE = PM.D(el, "ee"),
        EE50 = PM.D(el, "ee50"),
        EE60 = PM.D(el, "ee60"),
        EE70 = PM.D(el, "ee70"),
        EE80 = PM.D(el, "ee80"),
        EE90 = PM.D(el, "ee90"),
        Action = PM.D(el, "action"),
        ActionSSI = PM.D(el, "actionSSI"),
        Equity = PM.D(el, "equity"),
        EquitySSI = PM.D(el, "equitySSI"),
        EECash = PM.D(el, "eeCash"),
        HoldSubscription = PM.D(el, "holdSubscription"),
        BankBalance = PM.D(el, "bankBalance"),
        OnHoldCash = PM.D(el, "onHoldCash"),
        Doverdue = PM.D(el, "doverdue"),
        DoverdueSSI = PM.D(el, "doverdue_ssi", "doverdueSSI"),
        AccountBalance = PM.D(el, "accountBalance"),
        D = PM.D(el, "D", "d"),
        DSpv = PM.D(el, "dSPV", "dspv"),
        DSsi = PM.D(el, "dSSI", "dssi"),
        Cia = PM.D(el, "cia"),
        CollateralAsset = PM.D(el, "collateralAsset"),
        CollateralAssetSSI = PM.D(el, "collateralAssetSSI"),
        TotalAssets = PM.D(el, "totalAssets"),
        TotalEquity = PM.D(el, "totalEquity"),
        TotalEquitySSI = PM.D(el, "totalEquitySSI"),
        Lmv = PM.D(el, "lmv"),
        LmvMargin = PM.D(el, "lmvMargin"),
        LmvMarginSSI = PM.D(el, "lmvMarginSSI"),
        CallLmv = PM.D(el, "callLMV", "callLmv"),
        ForceLmv = PM.D(el, "forceLMV", "forceLmv"),
        CallLmvSSI = PM.D(el, "callLMVSSI", "callLmvSSI"),
        ForceLmvSSI = PM.D(el, "forceLMVSSI", "forceLmvSSI"),
        LmvNonMarginable = PM.D(el, "lmvNonMarginable"),
        LmvNonMarginableSSI = PM.D(el, "lmvNonMarginableSSI"),
        PreLoan = PM.D(el, "preLoan"),
        MarginRatio = PM.D(el, "marginRatio"),
        MarginRatioSSI = PM.D(el, "marginRatioSSI"),
        PurchasingPower = PM.D(el, "purchasingPower"),
        EeOrigin = PM.D(el, "eeOrigin"),
        BuyUnmatched = PM.D(el, "buyUnmatched"),
        SellUnmatched = PM.D(el, "sellUnmatched"),
        BuyT0 = PM.D(el, "buyT0"),
        BuyT1 = PM.D(el, "buyT1"),
        BuyT2 = PM.D(el, "buyT2"),
        SellT0 = PM.D(el, "sellT0"),
        SellT1 = PM.D(el, "sellT1"),
        SellT2 = PM.D(el, "sellT2"),
        CreditLimit = PM.D(el, "creditLimit"),
        MarginCallLmvSold = PM.D(el, "marginCallLMVSold", "marginCallLmvSold"),
        MarginCallLmvSoldSSI = PM.D(el, "marginCallLMVSoldSSI", "marginCallLmvSoldSSI"),
        MarginCall = PM.D(el, "marginCall"),
        MarginCallSSI = PM.D(el, "marginCallSSI"),
        CollateralA = PM.D(el, "collateralA"),
        CollateralNon = PM.D(el, "collateralNon"),
        CollateralASSI = PM.D(el, "collateralASSI"),
        CollateralNonSSI = PM.D(el, "collateralNonSSI"),
        CallMargin = PM.D(el, "callMargin"),
        CallForceSell = PM.D(el, "callForcesell", "callForceSell"),
        CallMarginSSI = PM.D(el, "callMarginSSI"),
        CallForceSellSSI = PM.D(el, "callForcesellSSI", "callForceSellSSI"),
        Ar = PM.D(el, "ar"),
    };
}

public sealed class DerivativePPMMR
{
    public string AccountNo { get; set; } = string.Empty;
    public decimal AccountBalance { get; set; }
    public decimal Fee { get; set; }
    public decimal Commission { get; set; }
    public decimal Interest { get; set; }
    public decimal Loan { get; set; }
    public decimal DeliveryAmount { get; set; }
    public decimal FloatingPL { get; set; }
    public decimal TradingPL { get; set; }
    public decimal TotalPL { get; set; }
    public decimal Marginable { get; set; }
    public decimal Depositable { get; set; }
    public decimal RcCall { get; set; }
    public decimal Withdrawable { get; set; }
    public decimal NonCashDrawableRcCall { get; set; }
    public decimal CashSSI { get; set; }
    public decimal ValidNonCashSSI { get; set; }
    public decimal TotalAssetSSI { get; set; }
    public decimal WithdrawableSSI { get; set; }
    public decimal EeSSI { get; set; }
    public decimal CashVSDC { get; set; }
    public decimal ValidNonCashVSDC { get; set; }
    public decimal TotalAssetVSDC { get; set; }
    public decimal WithdrawableVSDC { get; set; }
    public decimal EeVSDC { get; set; }
    public decimal SpreadMarginSSI { get; set; }
    public decimal DeliveryMarginSSI { get; set; }
    public decimal MarginReqSSI { get; set; }
    public decimal AccountRatioSSI { get; set; }
    public decimal UsedLimitWarningLevel1SSI { get; set; }
    public decimal UsedLimitWarningLevel2SSI { get; set; }
    public decimal UsedLimitWarningLevel3SSI { get; set; }
    public decimal MarginCallSSI { get; set; }
    public decimal SpreadMarginVSDC { get; set; }
    public decimal DeliveryMarginVSDC { get; set; }
    public decimal MarginReqVSDC { get; set; }
    public decimal AccountRatioVSDC { get; set; }
    public decimal UsedLimitWarningLevel1VSDC { get; set; }
    public decimal UsedLimitWarningLevel2VSDC { get; set; }
    public decimal UsedLimitWarningLevel3VSDC { get; set; }
    public decimal MarginCallVSDC { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal ExtInterest { get; set; }

    internal static DerivativePPMMR FromJson(JsonElement el) => new()
    {
        AccountNo = Converter.ToStr(Converter.GetProp(el, "accountNo")),
        AccountBalance = PM.D(el, "accountBalance"),
        Fee = PM.D(el, "fee"),
        Commission = PM.D(el, "commission"),
        Interest = PM.D(el, "interest"),
        Loan = PM.D(el, "loan"),
        DeliveryAmount = PM.D(el, "deliveryAmount"),
        FloatingPL = PM.D(el, "floatingPL"),
        TradingPL = PM.D(el, "tradingPL"),
        TotalPL = PM.D(el, "totalPL"),
        Marginable = PM.D(el, "marginable"),
        Depositable = PM.D(el, "depositable"),
        RcCall = PM.D(el, "rcCall"),
        Withdrawable = PM.D(el, "withdrawable"),
        NonCashDrawableRcCall = PM.D(el, "nonCashDrawableRCCall", "nonCashDrawableRcCall"),
        CashSSI = PM.D(el, "cashSSI"),
        ValidNonCashSSI = PM.D(el, "validNonCashSSI"),
        TotalAssetSSI = PM.D(el, "totalAssetSSI"),
        WithdrawableSSI = PM.D(el, "withdrawableSSI"),
        EeSSI = PM.D(el, "eeSSI"),
        CashVSDC = PM.D(el, "cashVSDC"),
        ValidNonCashVSDC = PM.D(el, "validNonCashVSDC"),
        TotalAssetVSDC = PM.D(el, "totalAssetVSDC"),
        WithdrawableVSDC = PM.D(el, "withdrawableVSDC"),
        EeVSDC = PM.D(el, "eeVSDC"),
        SpreadMarginSSI = PM.D(el, "spreadMarginSSI"),
        DeliveryMarginSSI = PM.D(el, "deliveryMarginSSI"),
        MarginReqSSI = PM.D(el, "marginReqSSI"),
        AccountRatioSSI = PM.D(el, "accountRatioSSI"),
        UsedLimitWarningLevel1SSI = PM.D(el, "usedLimitWarningLevel1SSI"),
        UsedLimitWarningLevel2SSI = PM.D(el, "usedLimitWarningLevel2SSI"),
        UsedLimitWarningLevel3SSI = PM.D(el, "usedLimitWarningLevel3SSI"),
        MarginCallSSI = PM.D(el, "marginCallSSI"),
        SpreadMarginVSDC = PM.D(el, "spreadMarginVSDC"),
        DeliveryMarginVSDC = PM.D(el, "deliveryMarginVSDC"),
        MarginReqVSDC = PM.D(el, "marginReqVSDC"),
        AccountRatioVSDC = PM.D(el, "accountRatioVSDC"),
        UsedLimitWarningLevel1VSDC = PM.D(el, "usedLimitWarningLevel1VSDC"),
        UsedLimitWarningLevel2VSDC = PM.D(el, "usedLimitWarningLevel2VSDC"),
        UsedLimitWarningLevel3VSDC = PM.D(el, "usedLimitWarningLevel3VSDC"),
        MarginCallVSDC = PM.D(el, "marginCallVSDC"),
        TotalEquity = PM.D(el, "totalEquity"),
        ExtInterest = PM.D(el, "extInterest"),
    };
}

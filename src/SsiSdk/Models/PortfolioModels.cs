using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Models;

public sealed class EquityAccountBalance
{
    public string AccountNo { get; set; } = string.Empty;
    public double AvailableCash { get; set; }
    public double TotalDebt { get; set; }
    public double InterestLoan { get; set; }
    public double OverdueFeeLoan { get; set; }
    public double Withdrawal { get; set; }
    public double OnHoldCash { get; set; }
    public double SellUnmatched { get; set; }
    public double SellT0 { get; set; }
    public double SellT1 { get; set; }
    public double SellT2 { get; set; }
    public double BuyUnmatched { get; set; }
    public double BuyT0 { get; set; }
    public double BuyT1 { get; set; }
    public double BuyT2 { get; set; }
    public double AdvanceCashT0 { get; set; }
    public double AdvanceCashT1 { get; set; }
    public double HoldSubscription { get; set; }
    public double BankBalance { get; set; }
    public double Dividend { get; set; }
    public double DividendMargin { get; set; }
    public double BlockCash { get; set; }
    public double InterestCash { get; set; }
    public double LimitT0 { get; set; }
    public double TermDeposit { get; set; }

    internal static EquityAccountBalance FromJson(JsonElement el) => new()
    {
        AccountNo = Converter.ToStr(Converter.GetProp(el, "accountNo")),
        AvailableCash = Converter.ToFloat64(Converter.GetProp(el, "availableCash")),
        TotalDebt = Converter.ToFloat64(Converter.GetProp(el, "totalDebt")),
        InterestLoan = Converter.ToFloat64(Converter.GetProp(el, "interestLoan")),
        OverdueFeeLoan = Converter.ToFloat64(Converter.GetProp(el, "overdueFeeLoan")),
        Withdrawal = Converter.ToFloat64(Converter.GetProp(el, "withdrawal")),
        OnHoldCash = Converter.ToFloat64(Converter.GetProp(el, "onHoldCash")),
        SellUnmatched = Converter.ToFloat64(Converter.GetProp(el, "sellUnmatched")),
        SellT0 = Converter.ToFloat64(Converter.GetProp(el, "sellT0")),
        SellT1 = Converter.ToFloat64(Converter.GetProp(el, "sellT1")),
        SellT2 = Converter.ToFloat64(Converter.GetProp(el, "sellT2")),
        BuyUnmatched = Converter.ToFloat64(Converter.GetProp(el, "buyUnmatched")),
        BuyT0 = Converter.ToFloat64(Converter.GetProp(el, "buyT0")),
        BuyT1 = Converter.ToFloat64(Converter.GetProp(el, "buyT1")),
        BuyT2 = Converter.ToFloat64(Converter.GetProp(el, "buyT2")),
        AdvanceCashT0 = Converter.ToFloat64(Converter.GetProp(el, "advanceCashT0")),
        AdvanceCashT1 = Converter.ToFloat64(Converter.GetProp(el, "advanceCashT1")),
        HoldSubscription = Converter.ToFloat64(Converter.GetProp(el, "holdSubscription")),
        BankBalance = Converter.ToFloat64(Converter.GetProp(el, "bankBalance")),
        Dividend = Converter.ToFloat64(Converter.GetProp(el, "dividend")),
        DividendMargin = Converter.ToFloat64(Converter.GetProp(el, "dividendMargin")),
        BlockCash = Converter.ToFloat64(Converter.GetProp(el, "blockCash")),
        InterestCash = Converter.ToFloat64(Converter.GetProp(el, "interestCash")),
        LimitT0 = Converter.ToFloat64(Converter.GetProp(el, "limitT0")),
        TermDeposit = Converter.ToFloat64(Converter.GetProp(el, "termDeposit")),
    };
}

public sealed class DerivativeAccountBalance
{
    public string AccountNo { get; set; } = string.Empty;
    public double AccountBalance { get; set; }
    public double Fee { get; set; }
    public double Commission { get; set; }
    public double Interest { get; set; }
    public double ExtInterest { get; set; }
    public double Loan { get; set; }
    public double DeliveryAmount { get; set; }
    public double FloatingPL { get; set; }
    public double TradingPL { get; set; }
    public double TotalPL { get; set; }
    public double Withdrawable { get; set; }
    public double CashSSI { get; set; }
    public double ValidNonCashSSI { get; set; }
    public double CashWithdrawableSSI { get; set; }
    public double CashVSDC { get; set; }
    public double ValidNonCashVSDC { get; set; }
    public double CashWithdrawableVSDC { get; set; }

    internal static DerivativeAccountBalance FromJson(JsonElement el) => new()
    {
        AccountNo = Converter.ToStr(Converter.GetProp(el, "accountNo")),
        AccountBalance = Converter.ToFloat64(Converter.GetProp(el, "accountBalance")),
        Fee = Converter.ToFloat64(Converter.GetProp(el, "fee")),
        Commission = Converter.ToFloat64(Converter.GetProp(el, "commission")),
        Interest = Converter.ToFloat64(Converter.GetProp(el, "interest")),
        ExtInterest = Converter.ToFloat64(Converter.GetProp(el, "extInterest")),
        Loan = Converter.ToFloat64(Converter.GetProp(el, "loan")),
        DeliveryAmount = Converter.ToFloat64(Converter.GetProp(el, "deliveryAmount")),
        FloatingPL = Converter.ToFloat64(Converter.GetProp(el, "floatingPL")),
        TradingPL = Converter.ToFloat64(Converter.GetProp(el, "tradingPL")),
        TotalPL = Converter.ToFloat64(Converter.GetProp(el, "totalPL")),
        Withdrawable = Converter.ToFloat64(Converter.GetProp(el, "withdrawable")),
        CashSSI = Converter.ToFloat64(Converter.GetProp(el, "cashSSI")),
        ValidNonCashSSI = Converter.ToFloat64(Converter.GetProp(el, "validNonCashSSI")),
        CashWithdrawableSSI = Converter.ToFloat64(Converter.GetProp(el, "cashWithdrawableSSI")),
        CashVSDC = Converter.ToFloat64(Converter.GetProp(el, "cashVSDC")),
        ValidNonCashVSDC = Converter.ToFloat64(Converter.GetProp(el, "validNonCashVSDC")),
        CashWithdrawableVSDC = Converter.ToFloat64(Converter.GetProp(el, "cashWithdrawableVSDC")),
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
    public double CostPrice { get; set; }
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
        CostPrice = Converter.ToFloat64(Converter.GetProp(el, "costPrice")),
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
    public double BidAvgPrice { get; set; }
    public double AskAvgPrice { get; set; }
    public double TradePrice { get; set; }
    public double FloatingPL { get; set; }
    public double TradingPL { get; set; }

    internal static DerivativePosition FromJson(JsonElement el) => new()
    {
        AccountNo = Converter.ToStr(Converter.GetProp(el, "accountNo")),
        Symbol = Converter.ToStr(Converter.GetProp(el, "symbol")),
        Long = Converter.ToInt(Converter.GetProp(el, "long")),
        Short = Converter.ToInt(Converter.GetProp(el, "short")),
        Net = Converter.ToInt(Converter.GetProp(el, "net")),
        BidAvgPrice = Converter.ToFloat64(Converter.GetProp(el, "bidAvgPrice")),
        AskAvgPrice = Converter.ToFloat64(Converter.GetProp(el, "askAvgPrice")),
        TradePrice = Converter.ToFloat64(Converter.GetProp(el, "tradePrice")),
        FloatingPL = Converter.ToFloat64(Converter.GetProp(el, "floatingPL")),
        TradingPL = Converter.ToFloat64(Converter.GetProp(el, "tradingPL")),
    };

    internal static List<DerivativePosition> FromJsonArray(JsonElement? el) =>
        Converter.GetArray(el).Select(FromJson).ToList();
}

public sealed class AllDerivativePosition
{
    public List<DerivativePosition> OpenPositions { get; set; } = [];
    public List<DerivativePosition> ClosedPositions { get; set; } = [];

    internal static AllDerivativePosition FromJson(JsonElement el) => new()
    {
        OpenPositions = DerivativePosition.FromJsonArray(Converter.GetProp(el, "derOpenPositions")),
        ClosedPositions = DerivativePosition.FromJsonArray(Converter.GetProp(el, "derClosePositions"))
    };
}

public sealed class Order
{
    public string AccountNo { get; set; } = string.Empty;
    public string ClientRequestId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public double Price { get; set; }
    public double AvgPrice { get; set; }
    public int Quantity { get; set; }
    public int OsQuantity { get; set; }
    public int FilledQuantity { get; set; }
    public int CancelQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string InputTime { get; set; } = string.Empty;
    public string ModifyTime { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    internal static Order FromJson(JsonElement el, string accountNo) => new()
    {
        AccountNo = accountNo,
        ClientRequestId = Converter.ToStr(Converter.GetProp(el, "clientRequestId")),
        OrderId = Converter.ToStr(Converter.GetProp(el, "orderId")),
        Symbol = Converter.ToStr(Converter.GetProp(el, "symbol")),
        Side = Converter.ToStr(Converter.GetProp(el, "side")),
        OrderType = Converter.ToStr(Converter.GetProp(el, "orderType")),
        Price = Converter.ToFloat64(Converter.GetProp(el, "price")),
        AvgPrice = Converter.ToFloat64(Converter.GetProp(el, "avgPrice")),
        Quantity = Converter.ToInt(Converter.GetProp(el, "quantity")),
        OsQuantity = Converter.ToInt(Converter.GetProp(el, "osQuantity")),
        FilledQuantity = Converter.ToInt(Converter.GetProp(el, "filledQuantity")),
        CancelQuantity = Converter.ToInt(Converter.GetProp(el, "cancelQuantity")),
        Status = Converter.ToStr(Converter.GetProp(el, "orderStatus")),
        InputTime = Converter.ToStr(Converter.GetProp(el, "inputTime")),
        ModifyTime = Converter.ToStr(Converter.GetProp(el, "modifiedTime")),
        Message = Converter.ToStr(Converter.GetProp(el, "message")),
    };
}

public sealed class EquityPPMMR
{
    public string AccountNo { get; set; } = string.Empty;
    public double Dividend { get; set; }
    public double LoanValue { get; set; }
    public double TotalDebt { get; set; }
    public double Debt { get; set; }
    public double Liability { get; set; }
    public double LiabilitySSI { get; set; }
    public double NetLiability { get; set; }
    public double Fees { get; set; }
    public double InterestSSI { get; set; }
    public double InterestSPV { get; set; }
    public double Withdrawable { get; set; }
    public double EE { get; set; }
    public double EE50 { get; set; }
    public double EE60 { get; set; }
    public double EE70 { get; set; }
    public double EE80 { get; set; }
    public double EE90 { get; set; }
    public double Action { get; set; }
    public double ActionSSI { get; set; }
    public double Equity { get; set; }
    public double EquitySSI { get; set; }
    public double EECash { get; set; }
    public double HoldSubscription { get; set; }
    public double BankBalance { get; set; }
    public double OnHoldCash { get; set; }
    public double Doverdue { get; set; }
    public double DoverdueSSI { get; set; }
    public double AccountBalance { get; set; }
    public double D { get; set; }
    public double DSpv { get; set; }
    public double DSsi { get; set; }
    public double Cia { get; set; }
    public double CollateralAsset { get; set; }
    public double CollateralAssetSSI { get; set; }
    public double TotalAssets { get; set; }
    public double TotalEquity { get; set; }
    public double TotalEquitySSI { get; set; }
    public double Lmv { get; set; }
    public double LmvMargin { get; set; }
    public double LmvMarginSSI { get; set; }
    public double CallLmv { get; set; }
    public double ForceLmv { get; set; }
    public double CallLmvSSI { get; set; }
    public double ForceLmvSSI { get; set; }
    public double LmvNonMarginable { get; set; }
    public double LmvNonMarginableSSI { get; set; }
    public double PreLoan { get; set; }
    public double MarginRatio { get; set; }
    public double MarginRatioSSI { get; set; }
    public double PurchasingPower { get; set; }
    public double EeOrigin { get; set; }
    public double BuyUnmatched { get; set; }
    public double SellUnmatched { get; set; }
    public double BuyT0 { get; set; }
    public double BuyT1 { get; set; }
    public double BuyT2 { get; set; }
    public double SellT0 { get; set; }
    public double SellT1 { get; set; }
    public double SellT2 { get; set; }
    public double CreditLimit { get; set; }
    public double MarginCallLmvSold { get; set; }
    public double MarginCallLmvSoldSSI { get; set; }
    public double MarginCall { get; set; }
    public double MarginCallSSI { get; set; }
    public double CollateralA { get; set; }
    public double CollateralNon { get; set; }
    public double CollateralASSI { get; set; }
    public double CollateralNonSSI { get; set; }
    public double CallMargin { get; set; }
    public double CallForceSell { get; set; }
    public double CallMarginSSI { get; set; }
    public double CallForceSellSSI { get; set; }
    public double Ar { get; set; }

    internal static EquityPPMMR FromJson(JsonElement el) => new()
    {
        AccountNo = Converter.ToStr(Converter.GetProp(el, "accountNo")),
        Dividend = Converter.ToFloat64(Converter.GetProp(el, "dividend")),
        LoanValue = Converter.ToFloat64(Converter.GetProp(el, "loanValue")),
        TotalDebt = Converter.ToFloat64(Converter.GetProp(el, "totalDebt")),
        Debt = Converter.ToFloat64(Converter.GetProp(el, "debt")),
        Liability = Converter.ToFloat64(Converter.GetProp(el, "liability")),
        LiabilitySSI = Converter.ToFloat64(Converter.GetProp(el, "liabilitySSI")),
        NetLiability = Converter.ToFloat64(Converter.GetProp(el, "netLiability")),
        Fees = Converter.ToFloat64(Converter.GetProp(el, "fees")),
        InterestSSI = Converter.ToFloat64(Converter.GetProp(el, "interestSSI")),
        InterestSPV = Converter.ToFloat64(Converter.GetProp(el, "interestSPV")),
        Withdrawable = Converter.ToFloat64(Converter.GetProp(el, "withdrawable")),
        EE = Converter.ToFloat64(Converter.GetProp(el, "ee")),
        EE50 = Converter.ToFloat64(Converter.GetProp(el, "ee50")),
        EE60 = Converter.ToFloat64(Converter.GetProp(el, "ee60")),
        EE70 = Converter.ToFloat64(Converter.GetProp(el, "ee70")),
        EE80 = Converter.ToFloat64(Converter.GetProp(el, "ee80")),
        EE90 = Converter.ToFloat64(Converter.GetProp(el, "ee90")),
        Action = Converter.ToFloat64(Converter.GetProp(el, "action")),
        ActionSSI = Converter.ToFloat64(Converter.GetProp(el, "actionSSI")),
        Equity = Converter.ToFloat64(Converter.GetProp(el, "equity")),
        EquitySSI = Converter.ToFloat64(Converter.GetProp(el, "equitySSI")),
        EECash = Converter.ToFloat64(Converter.GetProp(el, "eeCash")),
        HoldSubscription = Converter.ToFloat64(Converter.GetProp(el, "holdSubscription")),
        BankBalance = Converter.ToFloat64(Converter.GetProp(el, "bankBalance")),
        OnHoldCash = Converter.ToFloat64(Converter.GetProp(el, "onHoldCash")),
        Doverdue = Converter.ToFloat64(Converter.GetProp(el, "doverdue")),
        DoverdueSSI = Converter.ToFloat64(Converter.GetProp(el, "doverdueSSI")),
        AccountBalance = Converter.ToFloat64(Converter.GetProp(el, "accountBalance")),
        D = Converter.ToFloat64(Converter.GetProp(el, "D")),
        DSpv = Converter.ToFloat64(Converter.GetProp(el, "dSPV")),
        DSsi = Converter.ToFloat64(Converter.GetProp(el, "dSSI")),
        Cia = Converter.ToFloat64(Converter.GetProp(el, "cia")),
        CollateralAsset = Converter.ToFloat64(Converter.GetProp(el, "collateralAsset")),
        CollateralAssetSSI = Converter.ToFloat64(Converter.GetProp(el, "collateralAssetSSI")),
        TotalAssets = Converter.ToFloat64(Converter.GetProp(el, "totalAssets")),
        TotalEquity = Converter.ToFloat64(Converter.GetProp(el, "totalEquity")),
        TotalEquitySSI = Converter.ToFloat64(Converter.GetProp(el, "totalEquitySSI")),
        Lmv = Converter.ToFloat64(Converter.GetProp(el, "lmv")),
        LmvMargin = Converter.ToFloat64(Converter.GetProp(el, "lmvMargin")),
        LmvMarginSSI = Converter.ToFloat64(Converter.GetProp(el, "lmvMarginSSI")),
        CallLmv = Converter.ToFloat64(Converter.GetProp(el, "callLmv")),
        ForceLmv = Converter.ToFloat64(Converter.GetProp(el, "forceLmv")),
        CallLmvSSI = Converter.ToFloat64(Converter.GetProp(el, "callLmvSSI")),
        ForceLmvSSI = Converter.ToFloat64(Converter.GetProp(el, "forceLmvSSI")),
        LmvNonMarginable = Converter.ToFloat64(Converter.GetProp(el, "lmvNonMarginable")),
        LmvNonMarginableSSI = Converter.ToFloat64(Converter.GetProp(el, "lmvNonMarginableSSI")),
        PreLoan = Converter.ToFloat64(Converter.GetProp(el, "preLoan")),
        MarginRatio = Converter.ToFloat64(Converter.GetProp(el, "marginRatio")),
        MarginRatioSSI = Converter.ToFloat64(Converter.GetProp(el, "marginRatioSSI")),
        PurchasingPower = Converter.ToFloat64(Converter.GetProp(el, "purchasingPower")),
        EeOrigin = Converter.ToFloat64(Converter.GetProp(el, "eeOrigin")),
        BuyUnmatched = Converter.ToFloat64(Converter.GetProp(el, "buyUnmatched")),
        SellUnmatched = Converter.ToFloat64(Converter.GetProp(el, "sellUnmatched")),
        BuyT0 = Converter.ToFloat64(Converter.GetProp(el, "buyT0")),
        BuyT1 = Converter.ToFloat64(Converter.GetProp(el, "buyT1")),
        BuyT2 = Converter.ToFloat64(Converter.GetProp(el, "buyT2")),
        SellT0 = Converter.ToFloat64(Converter.GetProp(el, "sellT0")),
        SellT1 = Converter.ToFloat64(Converter.GetProp(el, "sellT1")),
        SellT2 = Converter.ToFloat64(Converter.GetProp(el, "sellT2")),
        CreditLimit = Converter.ToFloat64(Converter.GetProp(el, "creditLimit")),
        MarginCallLmvSold = Converter.ToFloat64(Converter.GetProp(el, "marginCallLmvSold")),
        MarginCallLmvSoldSSI = Converter.ToFloat64(Converter.GetProp(el, "marginCallLmvSoldSSI")),
        MarginCall = Converter.ToFloat64(Converter.GetProp(el, "marginCall")),
        MarginCallSSI = Converter.ToFloat64(Converter.GetProp(el, "marginCallSSI")),
        CollateralA = Converter.ToFloat64(Converter.GetProp(el, "collateralA")),
        CollateralNon = Converter.ToFloat64(Converter.GetProp(el, "collateralNon")),
        CollateralASSI = Converter.ToFloat64(Converter.GetProp(el, "collateralASSI")),
        CollateralNonSSI = Converter.ToFloat64(Converter.GetProp(el, "collateralNonSSI")),
        CallMargin = Converter.ToFloat64(Converter.GetProp(el, "callMargin")),
        CallForceSell = Converter.ToFloat64(Converter.GetProp(el, "callForceSell")),
        CallMarginSSI = Converter.ToFloat64(Converter.GetProp(el, "callMarginSSI")),
        CallForceSellSSI = Converter.ToFloat64(Converter.GetProp(el, "callForceSellSSI")),
        Ar = Converter.ToFloat64(Converter.GetProp(el, "ar")),
    };
}

public sealed class DerivativePPMMR
{
    public string AccountNo { get; set; } = string.Empty;
    public double AccountBalance { get; set; }
    public double Fee { get; set; }
    public double Commission { get; set; }
    public double Interest { get; set; }
    public double Loan { get; set; }
    public double DeliveryAmount { get; set; }
    public double FloatingPL { get; set; }
    public double TradingPL { get; set; }
    public double TotalPL { get; set; }
    public double Marginable { get; set; }
    public double Depositable { get; set; }
    public double RcCall { get; set; }
    public double Withdrawable { get; set; }
    public double NonCashDrawableRcCall { get; set; }
    public double CashSSI { get; set; }
    public double ValidNonCashSSI { get; set; }
    public double TotalAssetSSI { get; set; }
    public double WithdrawableSSI { get; set; }
    public double EeSSI { get; set; }
    public double CashVSDC { get; set; }
    public double ValidNonCashVSDC { get; set; }
    public double TotalAssetVSDC { get; set; }
    public double WithdrawableVSDC { get; set; }
    public double EeVSDC { get; set; }
    public double SpreadMarginSSI { get; set; }
    public double DeliveryMarginSSI { get; set; }
    public double MarginReqSSI { get; set; }
    public double AccountRatioSSI { get; set; }
    public double UsedLimitWarningLevel1SSI { get; set; }
    public double UsedLimitWarningLevel2SSI { get; set; }
    public double UsedLimitWarningLevel3SSI { get; set; }
    public double MarginCallSSI { get; set; }
    public double SpreadMarginVSDC { get; set; }
    public double DeliveryMarginVSDC { get; set; }
    public double MarginReqVSDC { get; set; }
    public double AccountRatioVSDC { get; set; }
    public double UsedLimitWarningLevel1VSDC { get; set; }
    public double UsedLimitWarningLevel2VSDC { get; set; }
    public double UsedLimitWarningLevel3VSDC { get; set; }
    public double MarginCallVSDC { get; set; }
    public double TotalEquity { get; set; }
    public double ExtInterest { get; set; }

    internal static DerivativePPMMR FromJson(JsonElement el) => new()
    {
        AccountNo = Converter.ToStr(Converter.GetProp(el, "accountNo")),
        AccountBalance = Converter.ToFloat64(Converter.GetProp(el, "accountBalance")),
        Fee = Converter.ToFloat64(Converter.GetProp(el, "fee")),
        Commission = Converter.ToFloat64(Converter.GetProp(el, "commission")),
        Interest = Converter.ToFloat64(Converter.GetProp(el, "interest")),
        Loan = Converter.ToFloat64(Converter.GetProp(el, "loan")),
        DeliveryAmount = Converter.ToFloat64(Converter.GetProp(el, "deliveryAmount")),
        FloatingPL = Converter.ToFloat64(Converter.GetProp(el, "floatingPL")),
        TradingPL = Converter.ToFloat64(Converter.GetProp(el, "tradingPL")),
        TotalPL = Converter.ToFloat64(Converter.GetProp(el, "totalPL")),
        Marginable = Converter.ToFloat64(Converter.GetProp(el, "marginable")),
        Depositable = Converter.ToFloat64(Converter.GetProp(el, "depositable")),
        RcCall = Converter.ToFloat64(Converter.GetProp(el, "rcCall")),
        Withdrawable = Converter.ToFloat64(Converter.GetProp(el, "withdrawable")),
        NonCashDrawableRcCall = Converter.ToFloat64(Converter.GetProp(el, "nonCashDrawableRcCall")),
        CashSSI = Converter.ToFloat64(Converter.GetProp(el, "cashSSI")),
        ValidNonCashSSI = Converter.ToFloat64(Converter.GetProp(el, "validNonCashSSI")),
        TotalAssetSSI = Converter.ToFloat64(Converter.GetProp(el, "totalAssetSSI")),
        WithdrawableSSI = Converter.ToFloat64(Converter.GetProp(el, "withdrawableSSI")),
        EeSSI = Converter.ToFloat64(Converter.GetProp(el, "eeSSI")),
        CashVSDC = Converter.ToFloat64(Converter.GetProp(el, "cashVSDC")),
        ValidNonCashVSDC = Converter.ToFloat64(Converter.GetProp(el, "validNonCashVSDC")),
        TotalAssetVSDC = Converter.ToFloat64(Converter.GetProp(el, "totalAssetVSDC")),
        WithdrawableVSDC = Converter.ToFloat64(Converter.GetProp(el, "withdrawableVSDC")),
        EeVSDC = Converter.ToFloat64(Converter.GetProp(el, "eeVSDC")),
        SpreadMarginSSI = Converter.ToFloat64(Converter.GetProp(el, "spreadMarginSSI")),
        DeliveryMarginSSI = Converter.ToFloat64(Converter.GetProp(el, "deliveryMarginSSI")),
        MarginReqSSI = Converter.ToFloat64(Converter.GetProp(el, "marginReqSSI")),
        AccountRatioSSI = Converter.ToFloat64(Converter.GetProp(el, "accountRatioSSI")),
        UsedLimitWarningLevel1SSI = Converter.ToFloat64(Converter.GetProp(el, "usedLimitWarningLevel1SSI")),
        UsedLimitWarningLevel2SSI = Converter.ToFloat64(Converter.GetProp(el, "usedLimitWarningLevel2SSI")),
        UsedLimitWarningLevel3SSI = Converter.ToFloat64(Converter.GetProp(el, "usedLimitWarningLevel3SSI")),
        MarginCallSSI = Converter.ToFloat64(Converter.GetProp(el, "marginCallSSI")),
        SpreadMarginVSDC = Converter.ToFloat64(Converter.GetProp(el, "spreadMarginVSDC")),
        DeliveryMarginVSDC = Converter.ToFloat64(Converter.GetProp(el, "deliveryMarginVSDC")),
        MarginReqVSDC = Converter.ToFloat64(Converter.GetProp(el, "marginReqVSDC")),
        AccountRatioVSDC = Converter.ToFloat64(Converter.GetProp(el, "accountRatioVSDC")),
        UsedLimitWarningLevel1VSDC = Converter.ToFloat64(Converter.GetProp(el, "usedLimitWarningLevel1VSDC")),
        UsedLimitWarningLevel2VSDC = Converter.ToFloat64(Converter.GetProp(el, "usedLimitWarningLevel2VSDC")),
        UsedLimitWarningLevel3VSDC = Converter.ToFloat64(Converter.GetProp(el, "usedLimitWarningLevel3VSDC")),
        MarginCallVSDC = Converter.ToFloat64(Converter.GetProp(el, "marginCallVSDC")),
        TotalEquity = Converter.ToFloat64(Converter.GetProp(el, "totalEquity")),
        ExtInterest = Converter.ToFloat64(Converter.GetProp(el, "extInterest")),
    };
}

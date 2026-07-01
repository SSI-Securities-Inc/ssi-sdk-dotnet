namespace SsiSdk;

public static class Board
{
    public const string HOSE = "HOSE";
    public const string HNX = "HNX";
    public const string UPCOM = "UPCOM";
}

public static class Timeframe
{
    public const string Minute1 = "1m";
    public const string Minute3 = "3m";
    public const string Minute5 = "5m";
    public const string Minute15 = "15m";
    public const string Hour1 = "1h";
    public const string Day1 = "1d";
    public const string Week1 = "1w";
    public const string Month1 = "1M";
}

public static class OrderSide
{
    public const string Buy = "B";
    public const string Sell = "S";
}

public static class OrderType
{
    public const string ATO = "ATO";
    public const string ATC = "ATC";
    public const string LO = "LO";
    public const string MTL = "MTL";
    public const string MP = "MP";
    public const string MOK = "MOK";
    public const string MAK = "MAK";
    public const string PLO = "PLO";
}

public static class OrderStatus
{
    public const string Pending = "PD";
    public const string PendingApproval = "WA";
    public const string Ready = "RS";
    public const string Sent = "SD";
    public const string Queued = "QU";
    public const string Filled = "FF";
    public const string PartialFilled = "PF";
    public const string PartialCancelled = "FFPC";
    public const string PendingModify = "WM";
    public const string PendingCancel = "WC";
    public const string Cancelled = "CL";
    public const string Rejected = "RJ";
    public const string Expired = "EX";
    public const string PreSession = "IAV";
}

public static class AccountType
{
    public const string Equity = "Cash";
    public const string EquityMargin = "Margin";
    public const string Derivative = "Derivative";
}

internal static class StreamingMethod
{
    public const string Subscribe = "subscribe";
    public const string Unsubscribe = "unsubscribe";
    public const string PingPong = "ping_pong";
    public const string ListSubscription = "list_subscription";
}

internal static class StreamingChannel
{
    public const string Data = "DATA";
    public const string Heartbeat = "HEARTBEAT";
    public const string Trading = "TRADING";
}

public static class StreamingType
{
    public const string Order = "orderEvent";
    public const string OrderMatch = "orderMatchEvent";
    public const string Portfolio = "clientPortfolioEvent";
}

internal static class DataTopic
{
    public const string Quote = "quote.";
    public const string Trade = "trade.";
    public const string OddLot = "oddlot.";
    public const string Market = "market.";
    public const string Room = "room.";
    public const string Put = "put.";
}

public static class DataType
{
    public const string Quote = "quote";
    public const string Trade = "trade";
    public const string OddLot = "oddlot";
    public const string Market = "market";
    public const string Room = "room";
    public const string Put = "put";
}

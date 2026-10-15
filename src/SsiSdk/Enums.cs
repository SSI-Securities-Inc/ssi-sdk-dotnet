namespace SsiSdk;

public static class Board
{
    /// <summary>Canonical value for a value or member name in any case, or null when unknown.</summary>
    public static string? FromValue(string? raw) => EnumLookup.Find(typeof(Board), raw);

    public const string HOSE = "HOSE";
    public const string HNX = "HNX";
    public const string UPCOM = "UPCOM";

    /// <summary>Valid on the stream and master data only, not on the REST endpoints.</summary>
    public const string DERIVATIVES = "DERIVATIVES";
}

public static class Timeframe
{
    /// <summary>Canonical value for a value or member name in any case, or null when unknown.</summary>
    public static string? FromValue(string? raw) => EnumLookup.Find(typeof(Timeframe), raw);

    public const string Minute1 = "1m";
    public const string Minute3 = "3m";
    public const string Minute5 = "5m";
    public const string Minute15 = "15m";
    public const string Hour1 = "1h";
    public const string Day1 = "1d";
    public const string Week1 = "1w";
    public const string Month1 = "1M";

    /// <summary>Timeframes the OHLC endpoint accepts. 1w and 1M are rejected by the server (error 400210).</summary>
    public static readonly IReadOnlySet<string> Allowed = new HashSet<string> { Day1, Minute1, Minute3, Minute5, Minute15, Hour1 };
}

public static class OrderSide
{
    /// <summary>Canonical value for a value or member name in any case, or null when unknown.</summary>
    public static string? FromValue(string? raw) => EnumLookup.Find(typeof(OrderSide), raw);

    public const string Buy = "B";
    public const string Sell = "S";
}

public static class OrderType
{
    /// <summary>Canonical value for a value or member name in any case, or null when unknown.</summary>
    public static string? FromValue(string? raw) => EnumLookup.Find(typeof(OrderType), raw);

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
    /// <summary>Canonical value for a value or member name in any case, or null when unknown.</summary>
    public static string? FromValue(string? raw) => EnumLookup.Find(typeof(OrderStatus), raw);

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
    public const string Soi = "SOI";
    public const string Pas = "PAS";
    public const string Cpl = "CPL";
    public const string Requested = "RQ";
    public const string Error = "ERR";
}

public static class AccountType
{
    /// <summary>Canonical value for a value or member name in any case, or null when unknown.</summary>
    public static string? FromValue(string? raw) => EnumLookup.Find(typeof(AccountType), raw);

    public const string Equity = "Cash";
    public const string EquityMargin = "Margin";
    public const string Derivative = "Derivative";
}

public static class StreamingMethod
{
    /// <summary>Canonical value for a value or member name in any case, or null when unknown.</summary>
    public static string? FromValue(string? raw) => EnumLookup.Find(typeof(StreamingMethod), raw);

    public const string Subscribe = "subscribe";
    public const string Unsubscribe = "unsubscribe";
    public const string PingPong = "ping_pong";
    public const string ListSubscription = "list_subscription";
}

public static class StreamingChannel
{
    /// <summary>Canonical value for a value or member name in any case, or null when unknown.</summary>
    public static string? FromValue(string? raw) => EnumLookup.Find(typeof(StreamingChannel), raw);

    public const string Data = "DATA";
    public const string Heartbeat = "HEARTBEAT";
    public const string Trading = "TRADING";
}

public static class StreamingType
{
    /// <summary>Canonical value for a value or member name in any case, or null when unknown.</summary>
    public static string? FromValue(string? raw) => EnumLookup.Find(typeof(StreamingType), raw);

    public const string Order = "orderEvent";
    public const string OrderMatch = "orderMatchEvent";
    public const string Portfolio = "clientPortfolioEvent";
    public const string FCOOrder = "fcoOrderEvent";
}

public static class FCOType
{
    /// <summary>Canonical value for a value or member name in any case, or null when unknown.</summary>
    public static string? FromValue(string? raw) => EnumLookup.Find(typeof(FCOType), raw);

    public const string GTD = "gtd";
    public const string Stop = "stop";
    public const string StopLimit = "stop_limit";
    public const string TrailingStop = "trailing_stop";
    public const string TrailingStopLimit = "trailing_stop_limit";
    public const string OCO = "oco";
    public const string BullBear = "bullbear";
}

public static class FCOOperator
{
    /// <summary>Canonical value for a value or member name in any case, or null when unknown.</summary>
    public static string? FromValue(string? raw) => EnumLookup.Find(typeof(FCOOperator), raw);

    public const string Greater = "greater";
    public const string GreaterOrEqual = "greater_or_equal";
    public const string Lesser = "lesser";
    public const string LesserOrEqual = "lesser_or_equal";
    public const string Equal = "equal";
}

public static class FCOStatus
{
    /// <summary>Canonical value for a value or member name in any case, or null when unknown.</summary>
    public static string? FromValue(string? raw) => EnumLookup.Find(typeof(FCOStatus), raw);

    public const string Init = "INIT";
    public const string Wait = "WAIT";
    public const string Tri = "TRI";
    public const string Trit = "TRIT";
    public const string Ter = "TER";
    public const string Fis = "FIS";
    public const string Wc = "WC";
    public const string Exp = "EXP";
    public const string Err = "ERR";
}


public static class DataTopic
{
    /// <summary>Canonical value for a value or member name in any case, or null when unknown.</summary>
    public static string? FromValue(string? raw) => EnumLookup.Find(typeof(DataTopic), raw);

    public const string Quote = "quote.";
    public const string Trade = "trade.";
    public const string OddLot = "oddlot.";
    public const string Market = "market.";
    public const string Room = "room.";
    public const string Put = "put.";
    public const string IndexSummary = "indexsummary.";
}

public static class DataType
{
    /// <summary>Canonical value for a value or member name in any case, or null when unknown.</summary>
    public static string? FromValue(string? raw) => EnumLookup.Find(typeof(DataType), raw);

    public const string Quote = "quote";
    public const string Trade = "trade";
    public const string OddLot = "oddlot";
    public const string Market = "market";
    public const string Room = "room";
    public const string Put = "put";
    public const string IndexSummary = "indexsummary";
}

internal static class EnumLookup
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, (string Name, string Value)[]> Cache = new();

    public static string? Find(Type enumClass, string? raw)
    {
        if (raw is null) return null;
        var members = Cache.GetOrAdd(enumClass, t => t
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (f.Name, (string)f.GetRawConstantValue()!))
            .ToArray());
        foreach (var m in members)
            if (m.Value == raw) return m.Value;
        var upper = raw.ToUpperInvariant();
        var squashed = upper.Replace("_", "");
        foreach (var m in members)
            if (m.Value.ToUpperInvariant() == upper || m.Name.ToUpperInvariant() == squashed || m.Name.ToUpperInvariant() == upper)
                return m.Value;
        return null;
    }
}

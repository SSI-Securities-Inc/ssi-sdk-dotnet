using System.Text.RegularExpressions;

namespace SsiSdk;

/// <summary>Streaming topic builder: validates every topic against what the server accepts.</summary>
public static partial class Topic
{
    // asset.* / margin.* are not live on the server, so they are deliberately absent.
    public static readonly IReadOnlySet<string> TopicTypes = new HashSet<string>
    {
        "trade", "quote", "room", "put", "oddlot", "market", "order", "portfolio", "trade.index", "indexsummary",
    };

    public static readonly IReadOnlySet<string> StreamIntervals = new HashSet<string> { "tick", "1m", "5m" };

    /// <summary>
    /// The <c>@*</c> interval is how the server fans a basket index (VN30) or an exchange (HOSE) out to its symbols:
    /// without it <c>trade.VN30</c> is just an exact key that never matches a tick.
    /// </summary>
    public const string StreamIntervalAll = "*";

    private static readonly HashSet<string> IntervalTopicTypes = ["trade", "trade.index"];
    private static readonly HashSet<string> AllIntervalTopicTypes = ["trade", "quote", "room", "put", "oddlot"];
    private static readonly HashSet<string> AccountTopicTypes = ["order", "portfolio"];
    private static readonly Dictionary<string, string> Aliases = new() { ["UPCOMINDEX"] = "HNXUPCOMINDEX" };

    // One filter per topic: "*", a code, or a range A-B. The server does not split on commas.
    [GeneratedRegex(@"\A(?:\*|[A-Za-z0-9]+(?:-[A-Za-z0-9]+)?)\z")]
    private static partial Regex FilterRegex();

    [GeneratedRegex(@"\A[A-Za-z0-9]+\z")]
    private static partial Regex ConcreteRegex();

    /// <summary>
    /// Builds one topic, e.g. <c>trade.VNM@1m</c> or <c>trade.index.VN30</c>. The filter is <c>*</c>, a code or a range
    /// <c>A-B</c>, one per topic (a comma list is rejected; use <see cref="BuildMany"/>). The interval <c>*</c>
    /// (<see cref="StreamIntervalAll"/>) is the basket/exchange fan-out (<c>trade.VN30@*</c>, <c>quote.HOSE@*</c>):
    /// valid for trade, quote, room, put and oddlot with one concrete index or exchange code.
    /// </summary>
    public static string Build(string topicType, string filter = "*", string? interval = null)
    {
        if (!TopicTypes.Contains(topicType))
            throw new ValidationException($"unknown topic type '{topicType}'");
        if (filter is null || !FilterRegex().IsMatch(filter))
            throw new ValidationException($"invalid topic filter '{filter}'");
        if (!AccountTopicTypes.Contains(topicType))
            filter = filter.ToUpperInvariant();
        if (Aliases.TryGetValue(filter, out var alias)) filter = alias;

        var topic = $"{topicType}.{filter}";
        if (interval is null) return topic;
        var text = string.Equals(interval, "tick", StringComparison.OrdinalIgnoreCase) ? "tick" : interval;
        if (text == StreamIntervalAll)
        {
            if (!AllIntervalTopicTypes.Contains(topicType))
                throw new ValidationException($"interval '*' is not valid for {topicType}");
            if (!ConcreteRegex().IsMatch(filter))
                throw new ValidationException("interval '*' needs one concrete index or exchange code");
            return $"{topic}@{text}";
        }
        if (!IntervalTopicTypes.Contains(topicType))
            throw new ValidationException($"interval is only valid for trade and trade.index, not {topicType}");
        if (!StreamIntervals.Contains(text))
        {
            var allowed = string.Join(", ", StreamIntervals.OrderBy(x => x, StringComparer.Ordinal));
            throw new ValidationException($"interval must be one of {allowed}, got '{text}'");
        }
        if (topicType == "trade.index" && !ConcreteRegex().IsMatch(filter))
            throw new ValidationException("trade.index intervals need one concrete index, not a wildcard");
        return $"{topic}@{text}";
    }

    public static List<string> BuildMany(string topicType, IEnumerable<string>? filters, string? interval = null)
    {
        var list = filters?.ToList() ?? [];
        if (list.Count == 0)
            throw new ValidationException("at least one symbol is required");
        return list.Select(f => Build(topicType, f, interval)).ToList();
    }
}

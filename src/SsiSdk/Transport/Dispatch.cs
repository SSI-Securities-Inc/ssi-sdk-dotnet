using System.Text.Json;
using SsiSdk.Internal;

namespace SsiSdk.Transport;

/// <summary>Pure helpers for decoding and routing WebSocket frames (no I/O).</summary>
internal static class Dispatch
{
    private static readonly string[] AckCategories = ["Denied (account)", "Denied (scope)", "Invalid"];

    public static JsonElement? Decode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.ValueKind == JsonValueKind.Object ? doc.RootElement.Clone() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool Has(JsonElement message, string name) => message.TryGetProperty(name, out _);

    /// <summary>A reply to a request: has method and status but no topic.</summary>
    public static bool IsAck(JsonElement message) =>
        message.ValueKind == JsonValueKind.Object && Has(message, "method") && Has(message, "status") && !Has(message, "topic");

    /// <summary>Every route whose handler should see the frame, most specific first.</summary>
    public static List<string> RoutesOf(JsonElement message)
    {
        var routes = new List<string>();
        if (message.ValueKind != JsonValueKind.Object) return routes;
        if (message.TryGetProperty(Constants.WsKeyChannel, out var ch) && ch.ValueKind == JsonValueKind.String
            && !string.IsNullOrEmpty(ch.GetString()))
        {
            routes.Add(ch.GetString()!);
            if (IsAck(message)) routes.Add(Constants.WsRouteAck);
            return routes;
        }
        if (Has(message, "id")) routes.Add(Constants.WsRouteId);
        else if (Has(message, "code") && (Has(message, "msg") || Has(message, "message"))) routes.Add(Constants.WsRouteError);
        else if (!Has(message, "topic") && (Has(message, "trading") || Has(message, "data"))) routes.Add(Constants.WsRouteListSubscription);
        else if (IsAck(message)) routes.Add(Constants.WsRouteAck);
        return routes;
    }

    /// <summary>Why the server refused a request, from an error ack; null when it did not.</summary>
    public static string? AckErrorCategory(JsonElement message)
    {
        if (!IsAck(message)) return null;
        var status = Converter.ToStr(Converter.GetProp(message, "status"));
        if (!string.Equals(status, "error", StringComparison.OrdinalIgnoreCase)) return null;
        var text = Converter.ToStr(Converter.GetProp(message, "message"));
        foreach (var category in AckCategories)
        {
            if (text.Contains(category, StringComparison.OrdinalIgnoreCase)) return category;
        }
        return "error";
    }

    /// <summary>Parse a LIST_SUBSCRIPTION reply into trading and data topic lists.</summary>
    public static Dictionary<string, List<string>> SplitSubscriptions(JsonElement message)
    {
        var result = new Dictionary<string, List<string>>();
        foreach (var key in new[] { "trading", "data" })
        {
            var p = Converter.GetProp(message, key);
            var text = p is { ValueKind: JsonValueKind.String } v ? v.GetString() ?? "" : "";
            result[key] = text.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        return result;
    }
}

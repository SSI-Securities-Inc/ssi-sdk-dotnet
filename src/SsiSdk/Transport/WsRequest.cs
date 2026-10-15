using System.Text;
using System.Text.Json;

namespace SsiSdk.Transport;

/// <summary>One request frame: method, optional channel, optional topics.</summary>
internal sealed record WsRequest(string Method, string? Channel = null, IReadOnlyList<string>? Topics = null)
{
    public string ToJson()
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream))
        {
            w.WriteStartObject();
            w.WriteString("method", Method);
            if (Channel is not null) w.WriteString("channel", Channel);
            if (Topics is not null)
            {
                w.WriteStartArray("topics");
                foreach (var t in Topics) w.WriteStringValue(t);
                w.WriteEndArray();
            }
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Split into requests whose JSON stays within <paramref name="maxBytes"/> (one topic at least per part).</summary>
    public List<WsRequest> Split(int maxBytes = Constants.WsMaxFrameBytes)
    {
        if (Topics is null || Encoding.UTF8.GetByteCount(ToJson()) <= maxBytes) return [this];
        var parts = new List<WsRequest>();
        var current = new List<string>();
        foreach (var topic in Topics)
        {
            var trial = this with { Topics = [.. current, topic] };
            if (current.Count > 0 && Encoding.UTF8.GetByteCount(trial.ToJson()) > maxBytes)
            {
                parts.Add(this with { Topics = current });
                current = [];
            }
            current.Add(topic);
        }
        if (current.Count > 0) parts.Add(this with { Topics = current });
        return parts;
    }
}

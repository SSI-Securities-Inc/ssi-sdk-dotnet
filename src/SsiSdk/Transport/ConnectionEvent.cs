namespace SsiSdk;

/// <summary>State names carried by <see cref="ConnectionEvent"/>.</summary>
public static class ConnectionState
{
    public const string Connected = "connected";
    public const string Reconnected = "reconnected";
    public const string Disconnected = "disconnected";
    public const string Failed = "failed";
}

/// <summary>A change in the state of a streaming connection (see <c>StreamClient.OnConnection</c>).</summary>
/// <param name="State">One of the <see cref="ConnectionState"/> names.</param>
/// <param name="ReconnectCount">How many times the socket was re-opened after the first connect.</param>
/// <param name="Error">The error behind a <see cref="ConnectionState.Failed"/> state.</param>
public sealed record ConnectionEvent(string State, int ReconnectCount = 0, Exception? Error = null);

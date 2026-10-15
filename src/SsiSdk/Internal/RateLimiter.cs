using System.Diagnostics;

namespace SsiSdk.Internal;

/// <summary>Token bucket that can be capped to the server's limit and paused after a 429.</summary>
internal sealed class RateLimiter
{
    private readonly int _configured;
    private int _max;
    private double _tokens;
    private long _last;
    private long _pausedUntil;
    private readonly object _lock = new();

    public RateLimiter(int maxPerSecond)
    {
        _configured = maxPerSecond;
        _max = maxPerSecond;
        _tokens = maxPerSecond;
        _last = Stopwatch.GetTimestamp();
    }

    public int Rate
    {
        get { lock (_lock) return _max; }
    }

    /// <summary>Never go above the server's advertised limit. Ignored if either side is unlimited or invalid.</summary>
    public void Cap(int serverLimit)
    {
        if (serverLimit <= 0 || _configured <= 0) return;
        lock (_lock)
        {
            _max = Math.Min(_configured, serverLimit);
            _tokens = Math.Min(_tokens, _max);
        }
    }

    /// <summary>Block new requests for <paramref name="seconds"/>. An existing longer pause is never shortened.</summary>
    public void Pause(double seconds)
    {
        if (seconds <= 0) return;
        var until = Stopwatch.GetTimestamp() + (long)(seconds * Stopwatch.Frequency);
        lock (_lock)
        {
            if (until > _pausedUntil) _pausedUntil = until;
        }
    }

    /// <summary>Seconds to wait before a request may go out; 0 when one token was taken.</summary>
    internal double Reserve()
    {
        lock (_lock)
        {
            var now = Stopwatch.GetTimestamp();
            if (now < _pausedUntil)
                return (double)(_pausedUntil - now) / Stopwatch.Frequency;
            if (_max <= 0) return 0;
            var elapsed = (double)(now - _last) / Stopwatch.Frequency;
            _last = now;
            _tokens = Math.Min(_max, _tokens + elapsed * _max);
            if (_tokens >= 1)
            {
                _tokens--;
                return 0;
            }
            return (1.0 - _tokens) / _max;
        }
    }

    public void Acquire()
    {
        while (true)
        {
            var wait = Reserve();
            if (wait <= 0) return;
            Thread.Sleep(TimeSpan.FromSeconds(wait));
        }
    }

    public async Task AcquireAsync(CancellationToken ct = default)
    {
        while (true)
        {
            var wait = Reserve();
            if (wait <= 0) return;
            await Task.Delay(TimeSpan.FromSeconds(wait), ct).ConfigureAwait(false);
        }
    }
}

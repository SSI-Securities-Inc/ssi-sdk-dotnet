namespace SsiSdk.Internal;

internal sealed class RateLimiter
{
    private readonly int _max;
    private double _tokens;
    private DateTime _last;
    private readonly object _lock = new();

    public RateLimiter(int maxPerSecond)
    {
        _max = maxPerSecond;
        _tokens = maxPerSecond;
        _last = DateTime.UtcNow;
    }

    public void Acquire()
    {
        if (_max <= 0) return;
        while (true)
        {
            double waitSeconds;
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var elapsed = (now - _last).TotalSeconds;
                _last = now;
                _tokens = Math.Min(_max, _tokens + elapsed * _max);
                if (_tokens >= 1)
                {
                    _tokens--;
                    return;
                }
                var deficit = 1.0 - _tokens;
                waitSeconds = deficit / _max;
            }
            Thread.Sleep(TimeSpan.FromSeconds(waitSeconds));
        }
    }

    public async Task AcquireAsync(CancellationToken ct = default)
    {
        if (_max <= 0) return;
        while (true)
        {
            double waitSeconds;
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var elapsed = (now - _last).TotalSeconds;
                _last = now;
                _tokens = Math.Min(_max, _tokens + elapsed * _max);
                if (_tokens >= 1)
                {
                    _tokens--;
                    return;
                }
                var deficit = 1.0 - _tokens;
                waitSeconds = deficit / _max;
            }
            await Task.Delay(TimeSpan.FromSeconds(waitSeconds), ct);
        }
    }
}

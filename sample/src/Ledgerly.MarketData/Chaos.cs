namespace Ledgerly.MarketData;

public sealed class ChaosOptions
{
    /// <summary>0.0 - 1.0: share of /prices requests that fail with 503.</summary>
    public double FailureRate { get; set; }

    /// <summary>Extra delay added to every /prices request.</summary>
    public int LatencyMs { get; set; }
}

/// <summary>Runtime-adjustable chaos settings (starts from configuration, can be changed via POST /chaos).</summary>
public sealed class ChaosState
{
    private readonly Lock _gate = new();
    private ChaosOptions _current;

    public ChaosState(ChaosOptions initial) => _current = initial;

    public ChaosOptions Current
    {
        get { lock (_gate) { return new ChaosOptions { FailureRate = _current.FailureRate, LatencyMs = _current.LatencyMs }; } }
    }

    public void Set(ChaosOptions options)
    {
        lock (_gate)
        {
            _current = new ChaosOptions
            {
                FailureRate = Math.Clamp(options.FailureRate, 0, 1),
                LatencyMs = Math.Clamp(options.LatencyMs, 0, 60_000),
            };
        }
    }
}

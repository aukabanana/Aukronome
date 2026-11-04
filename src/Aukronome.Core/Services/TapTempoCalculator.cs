using Aukronome.Core.Models;

namespace Aukronome.Core.Services;

/// <summary>
/// Computes BPM from user tap timestamps by averaging the intervals between the last N taps,
/// with an adaptive inactivity reset timeout of max(2s, 1.5 × lastInterval) and clamping to 1–400 BPM.
/// </summary>
public sealed class TapTempoCalculator
{
    public static readonly TimeSpan DefaultMinTimeout = TimeSpan.FromSeconds(2.0);

    private readonly List<TimeSpan> _tapTimestamps = new();
    private readonly int _maxTaps;
    private readonly TimeSpan _minTimeout;
    private TimeSpan? _lastInterval;

    public int TapCount => _tapTimestamps.Count;

    public TapTempoCalculator(int maxTaps = 4, TimeSpan? minTimeout = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTaps, 2);
        _maxTaps = maxTaps;
        _minTimeout = minTimeout ?? DefaultMinTimeout;
    }

    /// <summary>
    /// Registers a tap at the current wall-clock time.
    /// Returns the computed BPM (1–400) if at least 2 taps are within the timeout window, or null on the first tap after reset.
    /// </summary>
    public int? RegisterTap() => RegisterTap(TimeSpan.FromTicks(StopwatchTimestampToTicks()));

    /// <summary>
    /// Registers a tap at an explicit timestamp (monotonic elapsed time).
    /// Returns the computed BPM (1–400) if at least 2 taps are within the timeout window, or null on the first tap after reset.
    /// </summary>
    public int? RegisterTap(TimeSpan timestamp)
    {
        if (_tapTimestamps.Count > 0)
        {
            TimeSpan elapsedSinceLast = timestamp - _tapTimestamps[^1];
            TimeSpan effectiveTimeout = GetEffectiveTimeout();

            if (elapsedSinceLast <= TimeSpan.Zero || elapsedSinceLast > effectiveTimeout)
            {
                Reset();
            }
        }

        _tapTimestamps.Add(timestamp);
        while (_tapTimestamps.Count > _maxTaps)
        {
            _tapTimestamps.RemoveAt(0);
        }

        if (_tapTimestamps.Count < 2)
        {
            return null;
        }

        _lastInterval = _tapTimestamps[^1] - _tapTimestamps[^2];

        // Average all intervals between consecutive retained taps:
        // sum of (t[i] - t[i-1]) for i = 1..count-1 equals (t[last] - t[first]) / (count - 1)
        int intervalCount = _tapTimestamps.Count - 1;
        double totalSeconds = (_tapTimestamps[^1] - _tapTimestamps[0]).TotalSeconds;
        double averageIntervalSeconds = totalSeconds / intervalCount;

        if (averageIntervalSeconds <= 1e-6)
        {
            return MetronomeParameters.MaxBpm;
        }

        double rawBpm = 60.0 / averageIntervalSeconds;
        int roundedBpm = (int)Math.Round(rawBpm, MidpointRounding.AwayFromZero);
        return Math.Clamp(roundedBpm, MetronomeParameters.MinBpm, MetronomeParameters.MaxBpm);
    }

    public TimeSpan GetEffectiveTimeout()
    {
        if (_lastInterval is null)
        {
            return _minTimeout;
        }

        TimeSpan scaled = TimeSpan.FromSeconds(_lastInterval.Value.TotalSeconds * 1.5);
        return scaled > _minTimeout ? scaled : _minTimeout;
    }

    public void Reset()
    {
        _tapTimestamps.Clear();
        _lastInterval = null;
    }

    private static long StopwatchTimestampToTicks()
    {
        double seconds = (double)System.Diagnostics.Stopwatch.GetTimestamp() / System.Diagnostics.Stopwatch.Frequency;
        return (long)(seconds * TimeSpan.TicksPerSecond);
    }
}

using Aukronome.Core.Services;
using Xunit;

namespace Aukronome.Core.Tests;

public class TapTempoCalculatorTests
{
    [Fact]
    public void RegisterTap_AveragesLastFourTaps_At120Bpm()
    {
        var calc = new TapTempoCalculator(maxTaps: 4);

        Assert.Null(calc.RegisterTap(TimeSpan.FromSeconds(0.0)));
        Assert.Equal(120, calc.RegisterTap(TimeSpan.FromSeconds(0.5)));
        Assert.Equal(120, calc.RegisterTap(TimeSpan.FromSeconds(1.0)));
        Assert.Equal(120, calc.RegisterTap(TimeSpan.FromSeconds(1.5)));
        Assert.Equal(4, calc.TapCount);
    }

    [Fact]
    public void RegisterTap_RollingWindowOfFourTaps_DropsOlderTaps()
    {
        var calc = new TapTempoCalculator(maxTaps: 4);

        // Start at 60 BPM (1.0s intervals)
        calc.RegisterTap(TimeSpan.FromSeconds(0.0));
        calc.RegisterTap(TimeSpan.FromSeconds(1.0));

        // Now tap 4 times at 120 BPM (0.5s intervals): 1.5, 2.0, 2.5, 3.0
        calc.RegisterTap(TimeSpan.FromSeconds(1.5));
        calc.RegisterTap(TimeSpan.FromSeconds(2.0));
        calc.RegisterTap(TimeSpan.FromSeconds(2.5));
        int? bpm = calc.RegisterTap(TimeSpan.FromSeconds(3.0));

        // Last 4 taps are [1.5, 2.0, 2.5, 3.0] -> exact 0.5s average -> 120 BPM
        Assert.Equal(120, bpm);
    }

    [Fact]
    public void RegisterTap_ResetsAfterTwoSecondInactivityAtNormalTempo()
    {
        var calc = new TapTempoCalculator();

        calc.RegisterTap(TimeSpan.FromSeconds(0.0));
        Assert.Equal(120, calc.RegisterTap(TimeSpan.FromSeconds(0.5)));

        // Wait 2.5 seconds (> 2.0s timeout) -> should reset and return null as first tap of new sequence
        int? afterPause = calc.RegisterTap(TimeSpan.FromSeconds(3.0));
        Assert.Null(afterPause);
        Assert.Equal(1, calc.TapCount);
    }

    [Fact]
    public void RegisterTap_AdaptiveTimeout_SupportsSlowTemposBelow30Bpm()
    {
        var calc = new TapTempoCalculator();

        // First two taps 1.8s apart (~33 BPM) -> effective timeout becomes max(2.0s, 1.5 * 1.8s = 2.7s)
        calc.RegisterTap(TimeSpan.FromSeconds(0.0));
        Assert.Equal(33, calc.RegisterTap(TimeSpan.FromSeconds(1.8)));

        // Next tap 2.5s later (24 BPM) is within 2.7s adaptive timeout, expanding timeout to 3.75s
        int? slowTap = calc.RegisterTap(TimeSpan.FromSeconds(4.3));
        Assert.NotNull(slowTap);

        // Subsequent taps at 3.0s intervals (20 BPM) continue smoothly without premature reset
        calc.RegisterTap(TimeSpan.FromSeconds(7.3));
        calc.RegisterTap(TimeSpan.FromSeconds(10.3));
        int? twentyBpm = calc.RegisterTap(TimeSpan.FromSeconds(13.3));
        Assert.Equal(20, twentyBpm);
    }

    [Fact]
    public void RegisterTap_ClampsTo1To400Range()
    {
        var calc = new TapTempoCalculator();

        calc.RegisterTap(TimeSpan.FromSeconds(0.0));
        // 0.05s interval = 1200 BPM -> clamped to 400
        Assert.Equal(400, calc.RegisterTap(TimeSpan.FromSeconds(0.05)));
    }
}

using Aukronome.Core.Audio;
using Aukronome.Core.Models;
using Xunit;

namespace Aukronome.Core.Tests;

public class MetronomeSampleProviderTests
{
    [Theory]
    [InlineData(120, 4, 4, SubdivisionType.None, 48000, 24000.0)]
    [InlineData(60, 3, 4, SubdivisionType.Eighths, 48000, 24000.0)]
    [InlineData(150, 6, 8, SubdivisionType.Triplets, 48000, 6400.0)]
    [InlineData(100, 5, 4, SubdivisionType.Sixteenths, 48000, 7200.0)]
    [InlineData(90, 7, 8, SubdivisionType.Quintuplets, 48000, 6400.0)]
    [InlineData(180, 4, 4, SubdivisionType.Sextuplets, 48000, 2666.6666666666665)]
    public void SampleProvider_SchedulesClicksAtExactSampleOffsets(
        int bpm,
        int numerator,
        int denominator,
        SubdivisionType subdivision,
        int sampleRate,
        double expectedSubIntervalFrames)
    {
        var parameters = new MetronomeParameters
        {
            Bpm = bpm,
            TimeSignature = new TimeSignature(numerator, denominator),
            Accents = MetronomeParameters.CreateDefaultAccents(numerator),
            Subdivision = subdivision,
            Sound = SoundType.Click,
            MasterVolume = 0.9f,
            SubdivisionVolume = 0.6f
        };

        var provider = new MetronomeSampleProvider(sampleRate, channels: 1, parameters);

        // Render enough frames for 2 full bars
        int totalTicksToVerify = numerator * subdivision.DivisionsPerBeat() * 2;
        int totalFramesToRender = (int)Math.Ceiling(totalTicksToVerify * expectedSubIntervalFrames) + 1024;
        var buffer = new float[totalFramesToRender];

        // Read in realistic 512-sample chunks to ensure timing is buffer-size independent
        int offset = 0;
        while (offset < totalFramesToRender)
        {
            int chunk = Math.Min(512, totalFramesToRender - offset);
            provider.Read(buffer, offset, chunk);
            offset += chunk;
        }

        var events = provider.GetRecordedEvents();
        Assert.True(events.Count >= totalTicksToVerify);

        for (int k = 0; k < totalTicksToVerify; k++)
        {
            long expectedFrame = (long)Math.Round(k * expectedSubIntervalFrames);
            Assert.Equal(expectedFrame, events[k].FrameIndex);

            // Also verify non-zero audio energy exists right at the scheduled onset frame
            Assert.True(MathF.Abs(buffer[expectedFrame]) > 0.01f,
                $"Expected audio onset at sample frame {expectedFrame} for click {k}.");
        }
    }

    [Fact]
    public void BpmChange_MidBar_AppliesFromNextClick()
    {
        const int sampleRate = 48000;
        var initial = new MetronomeParameters
        {
            Bpm = 120, // 24,000 frames per beat
            TimeSignature = new TimeSignature(4, 4),
            Subdivision = SubdivisionType.None
        };

        var provider = new MetronomeSampleProvider(sampleRate, channels: 1, initial);
        var scratch = new float[30000];

        // Render 10,000 frames: Beat 0 fired at frame 0; Beat 1 is scheduled for frame 24,000
        provider.Read(scratch, 0, 10000);

        // Change BPM mid-beat to 240 BPM (12,000 frames per beat)
        provider.UpdateParameters(initial with { Bpm = 240 });

        // Render another 30,000 frames (up to frame 40,000)
        provider.Read(scratch, 0, 30000);

        var events = provider.GetRecordedEvents();
        Assert.True(events.Count >= 3);

        // Beat 0 at 0, Beat 1 at 24,000, and from Beat 1 onward interval is 12,000 (Beat 2 at 36,000)
        Assert.Equal(0L, events[0].FrameIndex);
        Assert.Equal(24000L, events[1].FrameIndex);
        Assert.Equal(36000L, events[2].FrameIndex);
    }

    [Fact]
    public void TimeSignatureAndSubdivisionChanges_AreDeferredUntilNextBarLine()
    {
        const int sampleRate = 48000;
        var initial = new MetronomeParameters
        {
            Bpm = 120, // 24,000 frames per beat
            TimeSignature = new TimeSignature(4, 4),
            Subdivision = SubdivisionType.None
        };

        var provider = new MetronomeSampleProvider(sampleRate, channels: 1, initial);
        var scratch = new float[30000];

        // Render past Beat 0 and Beat 1 (30,000 frames -> currently in Bar 0, after Beat 1)
        provider.Read(scratch, 0, 30000);

        // Request change to 3/4 with Eighths subdivision mid-bar
        var updated = initial with
        {
            TimeSignature = new TimeSignature(3, 4),
            Accents = MetronomeParameters.CreateDefaultAccents(3),
            Subdivision = SubdivisionType.Eighths
        };
        provider.UpdateParameters(updated);

        // Render 150,000 more frames (total 180,000 frames)
        var rest = new float[150000];
        provider.Read(rest, 0, rest.Length);

        var events = provider.GetRecordedEvents();

        // Bar 0 must finish all 4 beats of 4/4 with Subdivision.None (events 0, 1, 2, 3)
        Assert.Equal(0, events[0].BarIndex);
        Assert.Equal(0, events[0].BeatIndex);
        Assert.Equal(4, events[0].BeatsPerBar);

        Assert.Equal(0, events[3].BarIndex);
        Assert.Equal(3, events[3].BeatIndex);
        Assert.Equal(4, events[3].BeatsPerBar);
        Assert.Equal(72000L, events[3].FrameIndex);

        // Bar 1 starts at frame 96,000 and must now be 3/4 with Eighths (12,000 frames per 8th)
        Assert.Equal(1, events[4].BarIndex);
        Assert.Equal(0, events[4].BeatIndex);
        Assert.Equal(0, events[4].SubdivisionIndex);
        Assert.Equal(3, events[4].BeatsPerBar);
        Assert.Equal(96000L, events[4].FrameIndex);

        Assert.Equal(1, events[5].BarIndex);
        Assert.Equal(0, events[5].BeatIndex);
        Assert.Equal(1, events[5].SubdivisionIndex);
        Assert.Equal(108000L, events[5].FrameIndex);
    }

    [Fact]
    public void AccentEdit_MidBar_AppliesImmediatelyAndMuteSilencesMainBeat()
    {
        const int sampleRate = 48000;
        var initial = new MetronomeParameters
        {
            Bpm = 120, // 24,000 frames per beat
            TimeSignature = new TimeSignature(4, 4),
            Accents = [AccentLevel.Accent, AccentLevel.Normal, AccentLevel.Normal, AccentLevel.Normal],
            Subdivision = SubdivisionType.None
        };

        var provider = new MetronomeSampleProvider(sampleRate, channels: 1, initial);
        var firstChunk = new float[12000];
        provider.Read(firstChunk, 0, firstChunk.Length);

        // Mute beat 1 (second beat) before frame 24,000 arrives
        provider.UpdateParameters(initial with
        {
            Accents = [AccentLevel.Accent, AccentLevel.Mute, AccentLevel.Normal, AccentLevel.Normal]
        });

        var secondChunk = new float[24000];
        provider.Read(secondChunk, 0, secondChunk.Length);

        // Beat 1 occurred at frame 24,000 (index 12,000 in secondChunk) and should be completely silent
        Assert.Equal(0f, secondChunk[12000], precision: 6);
    }

    [Fact]
    public void GetHeardBeatStateAtFrame_ReturnsLatencyCompensatedBeatAndPulse()
    {
        const int sampleRate = 48000;
        var provider = new MetronomeSampleProvider(sampleRate, channels: 1, new MetronomeParameters
        {
            Bpm = 120, // 24,000 frames per beat
            TimeSignature = new TimeSignature(4, 4)
        });

        var buffer = new float[96000];
        provider.Read(buffer, 0, buffer.Length);

        // At playedFrame = 500 (just after beat 0 onset), BeatIndex is 0 and pulse is lit
        var state0 = provider.GetHeardBeatStateAtFrame(500);
        Assert.True(state0.IsPlaying);
        Assert.Equal(0, state0.BeatIndex);
        Assert.True(state0.IsPulseLit);

        // At playedFrame = 25000 (just after beat 1 onset at 24000), BeatIndex is 1 and pulse is lit
        var state1 = provider.GetHeardBeatStateAtFrame(25000);
        Assert.Equal(1, state1.BeatIndex);
        Assert.True(state1.IsPulseLit);

        // Near the end of beat 1 (frame 47000), BeatIndex is still 1 but the flash pulse has decayed
        var state1Tail = provider.GetHeardBeatStateAtFrame(47000);
        Assert.Equal(1, state1Tail.BeatIndex);
        Assert.False(state1Tail.IsPulseLit);
    }

    [Theory]
    [InlineData(SoundType.Click)]
    [InlineData(SoundType.Beep)]
    [InlineData(SoundType.Woodblock)]
    [InlineData(SoundType.Cowbell)]
    [InlineData(SoundType.HiHat)]
    [InlineData(SoundType.Mechanical)]
    public void SoundSynthesizer_AllSoundTypes_GenerateValidBuffers(SoundType soundType)
    {
        const int sampleRate = 48000;
        var bank = SoundSynthesizer.CreateBank(soundType, sampleRate);

        Assert.NotNull(bank);
        Assert.Equal(soundType, bank.SoundType);
        Assert.True(bank.Accent.Length > 0);
        Assert.True(bank.Normal.Length > 0);
        Assert.True(bank.Subdivision.Length > 0);

        Assert.Contains(bank.Accent, s => MathF.Abs(s) > 0.1f);
        Assert.Contains(bank.Normal, s => MathF.Abs(s) > 0.1f);
        Assert.Contains(bank.Subdivision, s => MathF.Abs(s) > 0.1f);
    }
}

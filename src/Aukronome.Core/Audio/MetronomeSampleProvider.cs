using Aukronome.Core.Models;
using NAudio.Wave;

namespace Aukronome.Core.Audio;

/// <summary>
/// Represents a click event scheduled at an exact sample frame offset.
/// </summary>
public readonly record struct ScheduledClickEvent(
    long FrameIndex,
    int BarIndex,
    int BeatIndex,
    int SubdivisionIndex,
    int BeatsPerBar,
    AccentLevel Accent,
    double BeatDurationFrames);

/// <summary>
/// Represents the musical beat state heard at a specific playback sample frame.
/// </summary>
public readonly record struct HeardBeatState(
    bool IsPlaying,
    int BarIndex,
    int BeatIndex,
    int SubdivisionIndex,
    int BeatsPerBar,
    AccentLevel Accent,
    double BeatPhase,
    bool IsPulseLit);

/// <summary>
/// Sample-accurate NAudio <see cref="ISampleProvider"/> that synthesizes and mixes metronome clicks
/// at exact sample frame offsets without relying on UI or OS timers.
/// </summary>
public sealed class MetronomeSampleProvider : ISampleProvider
{
    private const int EventHistoryCapacity = 512;

    private readonly object _syncLock = new();
    private readonly Dictionary<SoundType, SynthesizedSoundBank> _soundBanks = new();
    private readonly List<ActiveVoice> _activeVoices = new(16);
    private readonly ScheduledClickEvent[] _eventRingBuffer = new ScheduledClickEvent[EventHistoryCapacity];

    private int _eventWriteIndex;
    private int _eventCount;

    private MetronomeParameters _activeParams;
    private MetronomeParameters _pendingParams;
    private bool _hasPendingBarBoundaryChange;
    private bool _hasStartedRendering;

    private long _totalFramesRendered;
    private double _nextClickFrameExact;
    private int _currentBarIndex;
    private int _currentBeatIndex;
    private int _currentSubdivisionIndex;

    public WaveFormat WaveFormat { get; }
    public int SampleRate => WaveFormat.SampleRate;
    public int Channels => WaveFormat.Channels;
    public long TotalFramesRendered
    {
        get { lock (_syncLock) return _totalFramesRendered; }
    }

    public MetronomeSampleProvider(int sampleRate = 48000, int channels = 2, MetronomeParameters? initialParameters = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sampleRate, 8000);
        ArgumentOutOfRangeException.ThrowIfLessThan(channels, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(channels, 2);

        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);

        foreach (SoundType sound in Enum.GetValues<SoundType>())
        {
            _soundBanks[sound] = SoundSynthesizer.CreateBank(sound, sampleRate);
        }

        var normalized = (initialParameters ?? MetronomeParameters.CreateDefault()).Normalize();
        _activeParams = normalized;
        _pendingParams = normalized;
        Reset();
    }

    /// <summary>
    /// Resets playback state to Bar 0, Beat 0, Subdivision 0 at frame 0.
    /// Applies any pending parameter changes immediately.
    /// </summary>
    public void Reset()
    {
        lock (_syncLock)
        {
            _activeParams = _pendingParams;
            _hasPendingBarBoundaryChange = false;
            _hasStartedRendering = false;
            _totalFramesRendered = 0;
            _nextClickFrameExact = 0.0;
            _currentBarIndex = 0;
            _currentBeatIndex = 0;
            _currentSubdivisionIndex = 0;
            _activeVoices.Clear();
            _eventWriteIndex = 0;
            _eventCount = 0;
        }
    }

    /// <summary>
    /// Posts a new parameter snapshot to the engine.
    /// - BPM, Sound, Volumes, and same-signature Accent edits take effect immediately (from the next click).
    /// - Time-signature and Subdivision changes take effect at the next bar line if currently rendering.
    /// </summary>
    public void UpdateParameters(MetronomeParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var normalized = parameters.Normalize();

        lock (_syncLock)
        {
            _pendingParams = normalized;

            if (!_hasStartedRendering)
            {
                _activeParams = normalized;
                _hasPendingBarBoundaryChange = false;
                return;
            }

            bool signatureChanged = _activeParams.TimeSignature != normalized.TimeSignature;
            bool subdivisionChanged = _activeParams.Subdivision != normalized.Subdivision;

            if (signatureChanged || subdivisionChanged)
            {
                _hasPendingBarBoundaryChange = true;
            }

            // Immediate updates: BPM, Sound, Volumes, and Accents (if numerator matches active signature)
            var immediateAccents = _activeParams.Accents;
            if (normalized.Accents.Count == _activeParams.TimeSignature.Numerator)
            {
                immediateAccents = normalized.Accents;
            }
            else
            {
                // Copy overlapping beat accents so editing existing dots still works before bar boundary
                var merged = _activeParams.Accents.ToArray();
                int copyCount = Math.Min(merged.Length, normalized.Accents.Count);
                for (int i = 0; i < copyCount; i++)
                {
                    merged[i] = normalized.Accents[i];
                }
                immediateAccents = merged;
            }

            _activeParams = _activeParams with
            {
                Bpm = normalized.Bpm,
                Sound = normalized.Sound,
                MasterVolume = normalized.MasterVolume,
                SubdivisionVolume = normalized.SubdivisionVolume,
                Accents = immediateAccents
            };
        }
    }

    public MetronomeParameters GetActiveParameters()
    {
        lock (_syncLock) return _activeParams;
    }

    public MetronomeParameters GetPendingParameters()
    {
        lock (_syncLock) return _pendingParams;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int channels = WaveFormat.Channels;
        int framesRequested = count / channels;

        Array.Clear(buffer, offset, count);

        lock (_syncLock)
        {
            _hasStartedRendering = true;

            for (int f = 0; f < framesRequested; f++)
            {
                long currentFrame = _totalFramesRendered + f;
                long scheduledClickFrame = (long)Math.Round(_nextClickFrameExact);

                while (currentFrame >= scheduledClickFrame)
                {
                    TriggerClickAtFrame(currentFrame);
                    scheduledClickFrame = (long)Math.Round(_nextClickFrameExact);
                }

                float sampleSum = 0f;
                for (int v = _activeVoices.Count - 1; v >= 0; v--)
                {
                    var voice = _activeVoices[v];
                    sampleSum += voice.Samples[voice.Position] * voice.Gain;
                    voice.Position++;
                    if (voice.Position >= voice.Samples.Length)
                    {
                        _activeVoices.RemoveAt(v);
                    }
                    else
                    {
                        _activeVoices[v] = voice;
                    }
                }

                // Soft clip guard to prevent harsh digital wrap above +-1.0f
                sampleSum = Math.Clamp(sampleSum, -1.0f, 1.0f);

                int frameOffset = offset + f * channels;
                for (int c = 0; c < channels; c++)
                {
                    buffer[frameOffset + c] = sampleSum;
                }
            }

            _totalFramesRendered += framesRequested;
        }

        return framesRequested * channels;
    }

    private void TriggerClickAtFrame(long frameIndex)
    {
        // Apply deferred TimeSignature / Subdivision changes at the bar line (Beat 0, Sub 0)
        if (_currentBeatIndex == 0 && _currentSubdivisionIndex == 0 && _hasPendingBarBoundaryChange)
        {
            _activeParams = _pendingParams;
            _hasPendingBarBoundaryChange = false;
        }

        int numerator = _activeParams.TimeSignature.Numerator;
        int divisions = _activeParams.Subdivision.DivisionsPerBeat();
        AccentLevel beatAccent = _currentBeatIndex < _activeParams.Accents.Count
            ? _activeParams.Accents[_currentBeatIndex]
            : AccentLevel.Normal;

        double beatDurationFrames = (WaveFormat.SampleRate * 60.0) / _activeParams.Bpm;
        double subIntervalFrames = beatDurationFrames / divisions;

        // Record event for latency-compensated visual sync & unit test verification
        var clickEvent = new ScheduledClickEvent(
            FrameIndex: frameIndex,
            BarIndex: _currentBarIndex,
            BeatIndex: _currentBeatIndex,
            SubdivisionIndex: _currentSubdivisionIndex,
            BeatsPerBar: numerator,
            Accent: beatAccent,
            BeatDurationFrames: beatDurationFrames);

        _eventRingBuffer[_eventWriteIndex] = clickEvent;
        _eventWriteIndex = (_eventWriteIndex + 1) % EventHistoryCapacity;
        if (_eventCount < EventHistoryCapacity)
        {
            _eventCount++;
        }

        // Spawn synthesized audio voice
        var bank = _soundBanks[_activeParams.Sound];
        if (_currentSubdivisionIndex == 0)
        {
            // Main beat click
            if (beatAccent != AccentLevel.Mute && _activeParams.MasterVolume > 0f)
            {
                float[] samples = beatAccent == AccentLevel.Accent ? bank.Accent : bank.Normal;
                _activeVoices.Add(new ActiveVoice(samples, 0, _activeParams.MasterVolume));
            }
        }
        else
        {
            // Subdivision click
            float subGain = _activeParams.MasterVolume * _activeParams.SubdivisionVolume;
            if (subGain > 0f)
            {
                _activeVoices.Add(new ActiveVoice(bank.Subdivision, 0, subGain));
            }
        }

        // Advance musical position to next click
        _nextClickFrameExact += subIntervalFrames;
        _currentSubdivisionIndex++;
        if (_currentSubdivisionIndex >= divisions)
        {
            _currentSubdivisionIndex = 0;
            _currentBeatIndex++;
            if (_currentBeatIndex >= numerator)
            {
                _currentBeatIndex = 0;
                _currentBarIndex++;
            }
        }
    }

    private double GetSubdivisionIntervalFrames(int bpm, SubdivisionType subdivision)
    {
        int divisions = subdivision.DivisionsPerBeat();
        return (WaveFormat.SampleRate * 60.0) / (bpm * divisions);
    }

    /// <summary>
    /// Returns a snapshot of all recorded click events in chronological order (oldest to newest).
    /// Useful for deterministic unit tests and diagnostics.
    /// </summary>
    public IReadOnlyList<ScheduledClickEvent> GetRecordedEvents()
    {
        lock (_syncLock)
        {
            var result = new List<ScheduledClickEvent>(_eventCount);
            int start = (_eventWriteIndex - _eventCount + EventHistoryCapacity) % EventHistoryCapacity;
            for (int i = 0; i < _eventCount; i++)
            {
                int idx = (start + i) % EventHistoryCapacity;
                result.Add(_eventRingBuffer[idx]);
            }
            return result;
        }
    }

    /// <summary>
    /// Computes the latency-compensated visual beat state at the given hardware-played sample frame.
    /// </summary>
    public HeardBeatState GetHeardBeatStateAtFrame(long playedFrame)
    {
        lock (_syncLock)
        {
            if (_eventCount == 0 || playedFrame < 0)
            {
                return new HeardBeatState(
                    IsPlaying: false,
                    BarIndex: 0,
                    BeatIndex: -1,
                    SubdivisionIndex: 0,
                    BeatsPerBar: _activeParams.TimeSignature.Numerator,
                    Accent: AccentLevel.Normal,
                    BeatPhase: 0.0,
                    IsPulseLit: false);
            }

            int start = (_eventWriteIndex - _eventCount + EventHistoryCapacity) % EventHistoryCapacity;
            ScheduledClickEvent? latestEvent = null;
            ScheduledClickEvent? latestMainBeatEvent = null;

            for (int i = 0; i < _eventCount; i++)
            {
                int idx = (start + i) % EventHistoryCapacity;
                var ev = _eventRingBuffer[idx];
                if (ev.FrameIndex <= playedFrame)
                {
                    latestEvent = ev;
                    if (ev.SubdivisionIndex == 0)
                    {
                        latestMainBeatEvent = ev;
                    }
                }
                else
                {
                    break;
                }
            }

            if (latestEvent is null)
            {
                return new HeardBeatState(
                    IsPlaying: false,
                    BarIndex: 0,
                    BeatIndex: -1,
                    SubdivisionIndex: 0,
                    BeatsPerBar: _activeParams.TimeSignature.Numerator,
                    Accent: AccentLevel.Normal,
                    BeatPhase: 0.0,
                    IsPulseLit: false);
            }

            var current = latestEvent.Value;
            var mainBeat = latestMainBeatEvent ?? current;

            double framesSinceMainBeat = Math.Max(0, playedFrame - mainBeat.FrameIndex);
            double beatDuration = Math.Max(1.0, mainBeat.BeatDurationFrames);
            double beatPhase = Math.Clamp(framesSinceMainBeat / beatDuration, 0.0, 1.0);

            // Keep the visual pulse lit for the first 45% of the beat (clamped between 55 ms and 220 ms)
            // so even 1/4 time or very fast/slow tempos show a crisp visual flash on every beat.
            double pulseWindowFrames = Math.Clamp(
                beatDuration * 0.45,
                WaveFormat.SampleRate * 0.055,
                 Math.Min(beatDuration * 0.80, WaveFormat.SampleRate * 0.220));
            bool isPulseLit = framesSinceMainBeat <= pulseWindowFrames;

            return new HeardBeatState(
                IsPlaying: true,
                BarIndex: current.BarIndex,
                BeatIndex: current.BeatIndex,
                SubdivisionIndex: current.SubdivisionIndex,
                BeatsPerBar: current.BeatsPerBar,
                Accent: current.Accent,
                BeatPhase: beatPhase,
                IsPulseLit: isPulseLit);
        }
    }

    private struct ActiveVoice
    {
        public float[] Samples;
        public int Position;
        public float Gain;

        public ActiveVoice(float[] samples, int position, float gain)
        {
            Samples = samples;
            Position = position;
            Gain = gain;
        }
    }
}

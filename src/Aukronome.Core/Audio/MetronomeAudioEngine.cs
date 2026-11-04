using Aukronome.Core.Models;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Aukronome.Core.Audio;

/// <summary>
/// High-level metronome audio controller backed by NAudio WASAPI shared mode
/// and <see cref="MetronomeSampleProvider"/>.
/// </summary>
public sealed class MetronomeAudioEngine : IDisposable
{
    private readonly object _lifecycleLock = new();
    private readonly MetronomeSampleProvider _sampleProvider;
    private WasapiOut? _wasapiOut;
    private bool _isPlaying;
    private bool _disposed;

    public MetronomeSampleProvider SampleProvider => _sampleProvider;

    public bool IsPlaying
    {
        get { lock (_lifecycleLock) return _isPlaying; }
    }

    public MetronomeAudioEngine(int sampleRate = 48000, MetronomeParameters? initialParameters = null)
    {
        _sampleProvider = new MetronomeSampleProvider(sampleRate, channels: 2, initialParameters);
    }

    public void UpdateParameters(MetronomeParameters parameters)
    {
        _sampleProvider.UpdateParameters(parameters);
    }

    public void Start()
    {
        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_isPlaying)
            {
                return;
            }

            StopInternal();
            _sampleProvider.Reset();

            // WASAPI shared mode on default multimedia device (~35 ms buffer latency)
            _wasapiOut = new WasapiOut(AudioClientShareMode.Shared, useEventSync: true, latency: 35);
            _wasapiOut.Init(_sampleProvider);
            _wasapiOut.Play();
            _isPlaying = true;
        }
    }

    public void Stop()
    {
        lock (_lifecycleLock)
        {
            if (_disposed)
            {
                return;
            }
            StopInternal();
        }
    }

    public void Toggle()
    {
        lock (_lifecycleLock)
        {
            if (_isPlaying)
            {
                StopInternal();
            }
            else
            {
                Start();
            }
        }
    }

    /// <summary>
    /// Polls the WASAPI hardware playback position and returns the latency-compensated beat state
    /// corresponding to what is currently being heard through the speakers.
    /// </summary>
    public HeardBeatState GetHeardBeatState()
    {
        lock (_lifecycleLock)
        {
            if (!_isPlaying || _wasapiOut is null)
            {
                var active = _sampleProvider.GetActiveParameters();
                return new HeardBeatState(
                    IsPlaying: false,
                    BarIndex: 0,
                    BeatIndex: -1,
                    SubdivisionIndex: 0,
                    BeatsPerBar: active.TimeSignature.Numerator,
                    Accent: AccentLevel.Normal,
                    BeatPhase: 0.0,
                    IsPulseLit: false);
            }

            try
            {
                long bytesPlayed = _wasapiOut.GetPosition();
                int avgBytesPerSec = _wasapiOut.OutputWaveFormat.AverageBytesPerSecond;
                if (avgBytesPerSec <= 0)
                {
                    return _sampleProvider.GetHeardBeatStateAtFrame(0);
                }

                double secondsPlayed = (double)bytesPlayed / avgBytesPerSec;
                long playedFrame = (long)Math.Round(secondsPlayed * _sampleProvider.SampleRate);
                long maxRendered = Math.Max(0, _sampleProvider.TotalFramesRendered - 1);
                playedFrame = Math.Clamp(playedFrame, 0, maxRendered);

                return _sampleProvider.GetHeardBeatStateAtFrame(playedFrame);
            }
            catch
            {
                // Fallback if audio endpoint is mid-reconfiguration
                long fallbackFrame = Math.Max(0, _sampleProvider.TotalFramesRendered - 1);
                return _sampleProvider.GetHeardBeatStateAtFrame(fallbackFrame);
            }
        }
    }

    private void StopInternal()
    {
        _isPlaying = false;
        if (_wasapiOut is not null)
        {
            try
            {
                _wasapiOut.Stop();
            }
            catch
            {
                // Ignore device stop exceptions on disconnect
            }

            try
            {
                _wasapiOut.Dispose();
            }
            catch
            {
                // Ignore dispose exceptions
            }

            _wasapiOut = null;
        }
        _sampleProvider.Reset();
    }

    public void Dispose()
    {
        lock (_lifecycleLock)
        {
            if (_disposed)
            {
                return;
            }
            StopInternal();
            _disposed = true;
        }
    }
}

using Aukronome.Core.Models;

namespace Aukronome.Core.Audio;

/// <summary>
/// Holds the pre-rendered mono sample buffers for a single sound family at a specific sample rate.
/// </summary>
public sealed class SynthesizedSoundBank
{
    public SoundType SoundType { get; }
    public int SampleRate { get; }
    public float[] Accent { get; }
    public float[] Normal { get; }
    public float[] Subdivision { get; }

    public SynthesizedSoundBank(
        SoundType soundType,
        int sampleRate,
        float[] accent,
        float[] normal,
        float[] subdivision)
    {
        SoundType = soundType;
        SampleRate = sampleRate;
        Accent = accent;
        Normal = normal;
        Subdivision = subdivision;
    }
}

/// <summary>
/// Synthesizes built-in metronome sound banks in code with zero external asset files.
/// </summary>
public static class SoundSynthesizer
{
    public static SynthesizedSoundBank CreateBank(SoundType soundType, int sampleRate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sampleRate, 8000);

        return soundType switch
        {
            SoundType.Click => new SynthesizedSoundBank(
                soundType,
                sampleRate,
                SynthesizeClick(sampleRate, freqHz: 2400f, durationSec: 0.016f, peakGain: 0.95f),
                SynthesizeClick(sampleRate, freqHz: 1550f, durationSec: 0.014f, peakGain: 0.72f),
                SynthesizeClick(sampleRate, freqHz: 1100f, durationSec: 0.010f, peakGain: 0.55f)),

            SoundType.Beep => new SynthesizedSoundBank(
                soundType,
                sampleRate,
                SynthesizeBeep(sampleRate, freqHz: 1320f, durationSec: 0.048f, peakGain: 0.92f),
                SynthesizeBeep(sampleRate, freqHz: 880f, durationSec: 0.042f, peakGain: 0.70f),
                SynthesizeBeep(sampleRate, freqHz: 660f, durationSec: 0.028f, peakGain: 0.55f)),

            SoundType.Woodblock => new SynthesizedSoundBank(
                soundType,
                sampleRate,
                SynthesizeWoodblock(sampleRate, baseFreqHz: 1080f, durationSec: 0.042f, peakGain: 0.95f),
                SynthesizeWoodblock(sampleRate, baseFreqHz: 810f, durationSec: 0.036f, peakGain: 0.74f),
                SynthesizeWoodblock(sampleRate, baseFreqHz: 620f, durationSec: 0.024f, peakGain: 0.56f)),

            SoundType.Cowbell => new SynthesizedSoundBank(
                soundType,
                sampleRate,
                SynthesizeCowbell(sampleRate, f1Hz: 660f, f2Hz: 977f, durationSec: 0.068f, peakGain: 0.92f),
                SynthesizeCowbell(sampleRate, f1Hz: 540f, f2Hz: 800f, durationSec: 0.056f, peakGain: 0.72f),
                SynthesizeCowbell(sampleRate, f1Hz: 460f, f2Hz: 680f, durationSec: 0.032f, peakGain: 0.54f)),

            SoundType.HiHat => new SynthesizedSoundBank(
                soundType,
                sampleRate,
                SynthesizeHiHat(sampleRate, durationSec: 0.060f, decayRate: 65f, peakGain: 0.90f),
                SynthesizeHiHat(sampleRate, durationSec: 0.038f, decayRate: 95f, peakGain: 0.70f),
                SynthesizeHiHat(sampleRate, durationSec: 0.022f, decayRate: 140f, peakGain: 0.52f)),

            SoundType.Mechanical => new SynthesizedSoundBank(
                soundType,
                sampleRate,
                SynthesizeMechanicalAccent(sampleRate, peakGain: 0.80f),
                SynthesizeMechanicalNormal(sampleRate, peakGain: 0.78f),
                SynthesizeMechanicalSubdivision(sampleRate, peakGain: 0.54f)),

            _ => throw new ArgumentOutOfRangeException(nameof(soundType), soundType, "Unknown sound type.")
        };
    }

    private static float[] SynthesizeClick(int sampleRate, float freqHz, float durationSec, float peakGain)
    {
        int length = Math.Max(16, (int)(sampleRate * durationSec));
        var buffer = new float[length];
        double phase = 0.0;

        for (int i = 0; i < length; i++)
        {
            double t = (double)i / sampleRate;
            // Fast pitch drop on transient + sharp exponential envelope
            double instFreq = freqHz * (1.0 + 0.8 * Math.Exp(-t * 450.0));
            phase += 2.0 * Math.PI * instFreq / sampleRate;

            double env = Math.Exp(-t * 260.0);
            // Short initial impulse for crisp transient
            double impulse = i < Math.Max(2, sampleRate / 6000) ? 0.5 : 0.0;
            double sample = (Math.Sin(phase) * 0.85 + impulse) * env;
            buffer[i] = (float)sample;
        }

        NormalizeInPlace(buffer, peakGain);
        return buffer;
    }

    private static float[] SynthesizeBeep(int sampleRate, float freqHz, float durationSec, float peakGain)
    {
        int length = Math.Max(16, (int)(sampleRate * durationSec));
        var buffer = new float[length];
        double attackSec = Math.Min(0.002, durationSec * 0.15);

        for (int i = 0; i < length; i++)
        {
            double t = (double)i / sampleRate;
            double attack = t < attackSec ? t / attackSec : 1.0;
            double decay = Math.Exp(-t / (durationSec * 0.36));
            // Fundamental + subtle 2nd harmonic for clarity on small speakers
            double wave = Math.Sin(2.0 * Math.PI * freqHz * t) + 0.18 * Math.Sin(4.0 * Math.PI * freqHz * t);
            buffer[i] = (float)(wave * attack * decay);
        }

        NormalizeInPlace(buffer, peakGain);
        return buffer;
    }

    private static float[] SynthesizeWoodblock(int sampleRate, float baseFreqHz, float durationSec, float peakGain)
    {
        int length = Math.Max(16, (int)(sampleRate * durationSec));
        var buffer = new float[length];
        double phase1 = 0.0;
        double phase2 = 0.0;

        for (int i = 0; i < length; i++)
        {
            double t = (double)i / sampleRate;
            // Pitch bend at strike + two inharmonic wood modes (1.0 and 1.46)
            double pitchEnv = 1.0 + 0.35 * Math.Exp(-t * 380.0);
            double f1 = baseFreqHz * pitchEnv;
            double f2 = baseFreqHz * 1.46 * pitchEnv;

            phase1 += 2.0 * Math.PI * f1 / sampleRate;
            phase2 += 2.0 * Math.PI * f2 / sampleRate;

            double env1 = Math.Exp(-t * 115.0);
            double env2 = Math.Exp(-t * 240.0);
            double strike = (i < sampleRate / 2500) ? Math.Sin(2.0 * Math.PI * 3200.0 * t) * 0.35 : 0.0;

            buffer[i] = (float)(Math.Sin(phase1) * env1 + 0.45 * Math.Sin(phase2) * env2 + strike);
        }

        NormalizeInPlace(buffer, peakGain);
        return buffer;
    }

    private static float[] SynthesizeCowbell(int sampleRate, float f1Hz, float f2Hz, float durationSec, float peakGain)
    {
        int length = Math.Max(16, (int)(sampleRate * durationSec));
        var buffer = new float[length];

        for (int i = 0; i < length; i++)
        {
            double t = (double)i / sampleRate;
            // Band-limited odd-harmonic rich pair (classic 808 ratio ~1:1.48)
            double osc1 = Math.Sin(2.0 * Math.PI * f1Hz * t) + 0.33 * Math.Sin(6.0 * Math.PI * f1Hz * t);
            double osc2 = Math.Sin(2.0 * Math.PI * f2Hz * t) + 0.33 * Math.Sin(6.0 * Math.PI * f2Hz * t);

            double fastEnv = Math.Exp(-t * 140.0);
            double ringEnv = Math.Exp(-t * 48.0);
            double env = 0.55 * fastEnv + 0.45 * ringEnv;

            buffer[i] = (float)((osc1 + osc2) * 0.5 * env);
        }

        NormalizeInPlace(buffer, peakGain);
        return buffer;
    }

    private static float[] SynthesizeHiHat(int sampleRate, float durationSec, float decayRate, float peakGain)
    {
        int length = Math.Max(16, (int)(sampleRate * durationSec));
        var buffer = new float[length];
        // Deterministic seed so sound generation is reproducible in tests
        var rng = new Random(1337);
        double[] metallicFreqs = [3140.0, 4280.0, 5670.0, 7190.0, 8430.0];
        double prevNoise = 0.0;

        for (int i = 0; i < length; i++)
        {
            double t = (double)i / sampleRate;
            double white = rng.NextDouble() * 2.0 - 1.0;
            // Simple first-difference high-pass filter for crisp sizzle
            double highPassedNoise = white - 0.82 * prevNoise;
            prevNoise = white;

            double metallic = 0.0;
            for (int m = 0; m < metallicFreqs.Length; m++)
            {
                metallic += Math.Sign(Math.Sin(2.0 * Math.PI * metallicFreqs[m] * t));
            }
            metallic /= metallicFreqs.Length;

            double env = Math.Exp(-t * decayRate);
            buffer[i] = (float)((0.68 * highPassedNoise + 0.32 * metallic) * env);
        }

        NormalizeInPlace(buffer, peakGain);
        return buffer;
    }

    private static void NormalizeInPlace(float[] buffer, float targetPeak)
    {
        float maxAbs = 0f;
        for (int i = 0; i < buffer.Length; i++)
        {
            float abs = MathF.Abs(buffer[i]);
            if (abs > maxAbs)
            {
                maxAbs = abs;
            }
        }

        if (maxAbs <= 1e-6f)
        {
            return;
        }

        // Apply tiny 1.5 ms cosine fade-out at the tail to avoid truncation clicks
        int fadeSamples = Math.Min(buffer.Length / 4, 64);
        float scale = targetPeak / maxAbs;
        for (int i = 0; i < buffer.Length; i++)
        {
            float fade = 1f;
            int remaining = buffer.Length - 1 - i;
            if (remaining < fadeSamples && fadeSamples > 0)
            {
                fade = (float)remaining / fadeSamples;
            }
            buffer[i] *= scale * fade;
        }
    }

    private static float[] SynthesizeMechanicalNormal(int sampleRate, float peakGain)
    {
        // Classical wooden metronome escapement "Tok" (~42 ms)
        float durationSec = 0.042f;
        int length = Math.Max(16, (int)(sampleRate * durationSec));
        var buffer = new float[length];
        double phase1 = 0.0;
        double phase2 = 0.0;
        int clickImpulseSamples = Math.Max(2, (int)(sampleRate * 0.0018f));

        for (int i = 0; i < length; i++)
        {
            double t = (double)i / sampleRate;

            // 1. Initial mechanical escapement click impulse (sharp pallet release)
            double click = (i < clickImpulseSamples)
                ? Math.Sin(2.0 * Math.PI * 3400.0 * t) * 0.55
                : 0.0;

            // 2. Hollow wooden pyramid casing acoustic resonance (~440 Hz body fundamental with rapid decay, plus 1160 Hz box mode)
            double pitchDrop = 1.0 + 0.35 * Math.Exp(-t * 380.0);
            double f1 = 440.0 * pitchDrop;
            double f2 = 1160.0 * pitchDrop;

            phase1 += 2.0 * Math.PI * f1 / sampleRate;
            phase2 += 2.0 * Math.PI * f2 / sampleRate;

            double bodyEnv = Math.Exp(-t * 95.0);

            double sample = click + (Math.Sin(phase1) * 0.75 + Math.Sin(phase2) * 0.35) * bodyEnv;
            buffer[i] = (float)sample;
        }

        NormalizeInPlace(buffer, peakGain);
        return buffer;
    }

    private static float[] SynthesizeMechanicalAccent(int sampleRate, float peakGain)
    {
        // Warm acoustic mechanical downbeat bell chime + wooden body clack (~120 ms)
        // Mellow, rounded brass bell without piercing or harsh high-frequency shrieks
        float durationSec = 0.120f;
        int length = Math.Max(16, (int)(sampleRate * durationSec));
        var buffer = new float[length];

        // Escapement wooden body click
        float[] escapement = SynthesizeMechanicalNormal(sampleRate, peakGain: 0.72f);

        // Warm bell fundamental at 988 Hz (B5) and gentle 2nd harmonic at 1976 Hz (B6)
        // Kept strictly below 2 kHz to prevent ear fatigue and harsh treble piercing
        const double fBell1 = 987.77;
        const double fBell2 = 1975.53;
        int strikeSamples = Math.Max(2, (int)(sampleRate * 0.003f));

        for (int i = 0; i < length; i++)
        {
            double t = (double)i / sampleRate;
            double wood = (i < escapement.Length) ? escapement[i] : 0.0;

            // Soft felt/brass hammer tap (gentle 1800 Hz transient instead of harsh 4800 Hz)
            double strike = (i < strikeSamples)
                ? Math.Sin(2.0 * Math.PI * 1800.0 * t) * Math.Exp(-t * 400.0) * 0.18
                : 0.0;

            // Smooth musical chime decay
            double env1 = Math.Exp(-t * 22.0);
            double env2 = Math.Exp(-t * 46.0);

            double bell = (Math.Sin(2.0 * Math.PI * fBell1 * t) * 0.65 * env1)
                        + (Math.Sin(2.0 * Math.PI * fBell2 * t) * 0.20 * env2);

            // Rich wooden body carries the beat, with the mellow brass bell providing pleasant color
            buffer[i] = (float)(wood * 0.65 + (bell + strike) * 0.40);
        }

        NormalizeInPlace(buffer, peakGain);
        return buffer;
    }

    private static float[] SynthesizeMechanicalSubdivision(int sampleRate, float peakGain)
    {
        // Lighter, higher escapement tick (~20 ms)
        float durationSec = 0.020f;
        int length = Math.Max(16, (int)(sampleRate * durationSec));
        var buffer = new float[length];
        double phase = 0.0;
        int clickSamples = Math.Max(2, (int)(sampleRate * 0.0012f));

        for (int i = 0; i < length; i++)
        {
            double t = (double)i / sampleRate;
            double click = (i < clickSamples)
                ? Math.Sin(2.0 * Math.PI * 4200.0 * t) * 0.5
                : 0.0;

            double f = 1380.0 * (1.0 + 0.25 * Math.Exp(-t * 450.0));
            phase += 2.0 * Math.PI * f / sampleRate;
            double env = Math.Exp(-t * 180.0);

            buffer[i] = (float)(click + Math.Sin(phase) * 0.7 * env);
        }

        NormalizeInPlace(buffer, peakGain);
        return buffer;
    }

}

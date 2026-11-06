using System.Text.Json.Serialization;

namespace Aukronome.Core.Models;

/// <summary>
/// Per-beat accent state.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccentLevel
{
    Normal = 0,
    Accent = 1,
    Mute = 2
}

public static class AccentLevelExtensions
{
    /// <summary>
    /// Cycles Accent -> Normal -> Mute -> Accent.
    /// </summary>
    public static AccentLevel Next(this AccentLevel current) => current switch
    {
        AccentLevel.Accent => AccentLevel.Normal,
        AccentLevel.Normal => AccentLevel.Mute,
        AccentLevel.Mute => AccentLevel.Accent,
        _ => AccentLevel.Normal
    };
}

/// <summary>
/// Subdivision divisions per main beat.
/// The integer value equals the number of evenly-spaced subdivisions per beat.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubdivisionType
{
    None = 1,
    Eighths = 2,
    Triplets = 3,
    Sixteenths = 4,
    Quintuplets = 5,
    Sextuplets = 6
}

public static class SubdivisionTypeExtensions
{
    public static int DivisionsPerBeat(this SubdivisionType subdivision) =>
        Math.Clamp((int)subdivision, 1, 6);

    public static string DisplayName(this SubdivisionType subdivision) => subdivision switch
    {
        SubdivisionType.None => "None (1×)",
        SubdivisionType.Eighths => "8ths (2×)",
        SubdivisionType.Triplets => "Triplets (3×)",
        SubdivisionType.Sixteenths => "16ths (4×)",
        SubdivisionType.Quintuplets => "Quintuplets (5×)",
        SubdivisionType.Sextuplets => "Sextuplets (6×)",
        _ => subdivision.ToString()
    };
}

/// <summary>
/// Built-in synthesized click sound families.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SoundType
{
    Click = 0,
    Beep = 1,
    Woodblock = 2,
    Cowbell = 3,
    HiHat = 4,
    Mechanical = 5
}

public static class SoundTypeExtensions
{
    public static string DisplayName(this SoundType sound) => sound switch
    {
        SoundType.Click => "Click",
        SoundType.Beep => "Beep",
        SoundType.Woodblock => "Woodblock",
        SoundType.Cowbell => "Cowbell",
        SoundType.HiHat => "Hi-Hat",
        SoundType.Mechanical => "Mechanical (Classic)",
        _ => sound.ToString()
    };
}

/// <summary>
/// Represents a musical time signature (Numerator / Denominator).
/// BPM always counts the denominator note; every written beat clicks.
/// </summary>
public readonly record struct TimeSignature
{
    public const int MinNumerator = 1;
    public const int MaxNumerator = 32;
    public const int MinDenominator = 1;
    public const int MaxDenominator = 64;

    public int Numerator { get; init; }
    public int Denominator { get; init; }

    [JsonConstructor]
    public TimeSignature(int numerator, int denominator)
    {
        Numerator = Math.Clamp(numerator, MinNumerator, MaxNumerator);
        Denominator = Math.Clamp(denominator, MinDenominator, MaxDenominator);
    }

    public static TimeSignature FourFour => new(4, 4);

    public static bool TryParse(string? text, out TimeSignature signature)
    {
        signature = FourFour;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string cleaned = text.Trim();
        var parts = cleaned.Split(new[] { '/', ' ', '-', ',', ':' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out int num) &&
            int.TryParse(parts[1], out int den))
        {
            if (num >= MinNumerator && num <= MaxNumerator &&
                den >= MinDenominator && den <= MaxDenominator)
            {
                signature = new TimeSignature(num, den);
                return true;
            }
        }
        else if (parts.Length == 1 && int.TryParse(parts[0], out int singleNum))
        {
            if (singleNum >= MinNumerator && singleNum <= MaxNumerator)
            {
                signature = new TimeSignature(singleNum, 4);
                return true;
            }
        }

        return false;
    }

    public override string ToString() => $"{Numerator}/{Denominator}";
}

/// <summary>
/// Immutable snapshot of metronome musical & audio parameters.
/// </summary>
public sealed record MetronomeParameters
{
    public const int MinBpm = 1;
    public const int MaxBpm = 400;
    public const int DefaultBpm = 120;

    public int Bpm { get; init; } = DefaultBpm;
    public TimeSignature TimeSignature { get; init; } = TimeSignature.FourFour;
    public IReadOnlyList<AccentLevel> Accents { get; init; } = CreateDefaultAccents(4, 4);
    public SubdivisionType Subdivision { get; init; } = SubdivisionType.None;
    public SoundType Sound { get; init; } = SoundType.Click;
    public float MasterVolume { get; init; } = 0.85f;
    public float SubdivisionVolume { get; init; } = 0.55f;

    public static MetronomeParameters CreateDefault() => new();

    public static AccentLevel[] CreateDefaultAccents(int numerator) => CreateDefaultAccents(numerator, 4);

    public static AccentLevel[] CreateDefaultAccents(int numerator, int denominator)
    {
        int count = Math.Clamp(numerator, TimeSignature.MinNumerator, TimeSignature.MaxNumerator);
        var accents = new AccentLevel[count];
        for (int i = 0; i < count; i++)
        {
            accents[i] = AccentLevel.Normal;
        }

        if (count == 0) return accents;

        // Beat 1 is always accented
        accents[0] = AccentLevel.Accent;

        // Musical compound and odd meter groupings:
        if (denominator == 8)
        {
            switch (count)
            {
                case 6: // 3 + 3
                    accents[3] = AccentLevel.Accent;
                    break;
                case 8: // 3 + 3 + 2
                    accents[3] = AccentLevel.Accent;
                    accents[6] = AccentLevel.Accent;
                    break;
                case 7: // 2 + 2 + 3
                    accents[2] = AccentLevel.Accent;
                    accents[4] = AccentLevel.Accent;
                    break;
                case 9: // 3 + 3 + 3
                    accents[3] = AccentLevel.Accent;
                    accents[6] = AccentLevel.Accent;
                    break;
                case 12: // 3 + 3 + 3 + 3
                    accents[3] = AccentLevel.Accent;
                    accents[6] = AccentLevel.Accent;
                    accents[9] = AccentLevel.Accent;
                    break;
                case 5: // 3 + 2
                    accents[3] = AccentLevel.Accent;
                    break;
            }
        }
        else if (count == 5)
        {
            // 5/4: 3 + 2
            accents[3] = AccentLevel.Accent;
        }

        return accents;
    }

    public MetronomeParameters Normalize()
    {
        int clampedBpm = Math.Clamp(Bpm, MinBpm, MaxBpm);
        var sig = new TimeSignature(
            TimeSignature.Numerator == 0 ? 4 : TimeSignature.Numerator,
            TimeSignature.Denominator == 0 ? 4 : TimeSignature.Denominator);

        AccentLevel[] normalizedAccents;
        if (Accents is null || Accents.Count != sig.Numerator)
        {
            normalizedAccents = CreateDefaultAccents(sig.Numerator, sig.Denominator);
            if (Accents is not null)
            {
                int copyLen = Math.Min(Accents.Count, sig.Numerator);
                for (int i = 0; i < copyLen; i++)
                {
                    normalizedAccents[i] = Accents[i];
                }
            }
        }
        else
        {
            normalizedAccents = Accents.ToArray();
        }

        return this with
        {
            Bpm = clampedBpm,
            TimeSignature = sig,
            Accents = normalizedAccents,
            MasterVolume = Math.Clamp(MasterVolume, 0f, 1f),
            SubdivisionVolume = Math.Clamp(SubdivisionVolume, 0f, 1f)
        };
    }
}

using System.Text.Json;
using Aukronome.Core.Models;

namespace Aukronome.Core.Persistence;

/// <summary>
/// Manages loading and saving <see cref="AppSettings"/> (settings.json) and
/// <see cref="MetronomePreset"/> collections (presets.json) in %AppData%\Aukronome.
/// </summary>
public sealed class StorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public string StorageDirectory { get; }
    public string SettingsFilePath => Path.Combine(StorageDirectory, "settings.json");
    public string PresetsFilePath => Path.Combine(StorageDirectory, "presets.json");

    public StorageService(string? customDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(customDirectory))
        {
            StorageDirectory = customDirectory;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            StorageDirectory = Path.Combine(appData, "Aukronome");
        }
    }

    public AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return new AppSettings().Normalize();
            }

            string json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return (settings ?? new AppSettings()).Normalize();
        }
        catch
        {
            return new AppSettings().Normalize();
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(StorageDirectory);
        string json = JsonSerializer.Serialize(settings.Normalize(), JsonOptions);
        File.WriteAllText(SettingsFilePath, json);
    }

    public List<MetronomePreset> LoadPresets(bool seedDefaultsIfMissing = true)
    {
        try
        {
            if (!File.Exists(PresetsFilePath))
            {
                if (!seedDefaultsIfMissing)
                {
                    return [];
                }

                var defaults = CreateDefaultPresets();
                SavePresets(defaults);
                return defaults;
            }

            string json = File.ReadAllText(PresetsFilePath);
            var loaded = JsonSerializer.Deserialize<List<MetronomePreset>>(json, JsonOptions);
            if (loaded is null)
            {
                return [];
            }

            return loaded
                .Select(p => p.Normalize())
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }
        catch
        {
            return seedDefaultsIfMissing ? CreateDefaultPresets() : [];
        }
    }

    public void SavePresets(IEnumerable<MetronomePreset> presets)
    {
        ArgumentNullException.ThrowIfNull(presets);
        Directory.CreateDirectory(StorageDirectory);

        var normalized = presets
            .Select(p => p.Normalize())
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        string json = JsonSerializer.Serialize(normalized, JsonOptions);
        File.WriteAllText(PresetsFilePath, json);
    }

    public static List<MetronomePreset> CreateDefaultPresets() =>
    [
        new MetronomePreset
        {
            Name = "Standard 4/4 (120 BPM)",
            Parameters = new MetronomeParameters
            {
                Bpm = 120,
                TimeSignature = new TimeSignature(4, 4),
                Accents = [AccentLevel.Accent, AccentLevel.Normal, AccentLevel.Normal, AccentLevel.Normal],
                Subdivision = SubdivisionType.None,
                Sound = SoundType.Click,
                MasterVolume = 0.85f,
                SubdivisionVolume = 0.55f
            }
        },
        new MetronomePreset
        {
            Name = "Waltz 3/4 (96 BPM)",
            Parameters = new MetronomeParameters
            {
                Bpm = 96,
                TimeSignature = new TimeSignature(3, 4),
                Accents = [AccentLevel.Accent, AccentLevel.Normal, AccentLevel.Normal],
                Subdivision = SubdivisionType.None,
                Sound = SoundType.Woodblock,
                MasterVolume = 0.85f,
                SubdivisionVolume = 0.50f
            }
        },
        new MetronomePreset
        {
            Name = "Compound 6/8 (3+3, 168 BPM)",
            Parameters = new MetronomeParameters
            {
                Bpm = 168,
                TimeSignature = new TimeSignature(6, 8),
                Accents =
                [
                    AccentLevel.Accent, AccentLevel.Normal, AccentLevel.Normal,
                    AccentLevel.Accent, AccentLevel.Normal, AccentLevel.Normal
                ],
                Subdivision = SubdivisionType.None,
                Sound = SoundType.Click,
                MasterVolume = 0.85f,
                SubdivisionVolume = 0.50f
            }
        },
        new MetronomePreset
        {
            Name = "Odd 7/8 (2+2+3, 180 BPM)",
            Parameters = new MetronomeParameters
            {
                Bpm = 180,
                TimeSignature = new TimeSignature(7, 8),
                Accents =
                [
                    AccentLevel.Accent, AccentLevel.Normal,
                    AccentLevel.Accent, AccentLevel.Normal,
                    AccentLevel.Accent, AccentLevel.Normal, AccentLevel.Normal
                ],
                Subdivision = SubdivisionType.None,
                Sound = SoundType.Woodblock,
                MasterVolume = 0.85f,
                SubdivisionVolume = 0.50f
            }
        }
    ];
}

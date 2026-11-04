using Aukronome.Core.Models;
using Aukronome.Core.Persistence;
using Xunit;

namespace Aukronome.Core.Tests;

public class StorageServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly StorageService _storage;

    public StorageServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "AukronomeTests_" + Guid.NewGuid().ToString("N"));
        _storage = new StorageService(_tempDir);
    }

    [Fact]
    public void SaveAndLoadSettings_RoundTripsAllFields()
    {
        var customSettings = new AppSettings
        {
            LastParameters = new MetronomeParameters
            {
                Bpm = 144,
                TimeSignature = new TimeSignature(7, 8),
                Accents =
                [
                    AccentLevel.Accent, AccentLevel.Normal,
                    AccentLevel.Accent, AccentLevel.Normal,
                    AccentLevel.Accent, AccentLevel.Normal, AccentLevel.Mute
                ],
                Subdivision = SubdivisionType.Triplets,
                Sound = SoundType.Cowbell,
                MasterVolume = 0.75f,
                SubdivisionVolume = 0.40f
            },
            SelectedPresetName = "My Custom 7/8",
            WindowLeft = 120,
            WindowTop = 80,
            WindowWidth = 600,
            WindowHeight = 780
        };

        _storage.SaveSettings(customSettings);
        var loaded = _storage.LoadSettings();

        Assert.Equal(144, loaded.LastParameters.Bpm);
        Assert.Equal(new TimeSignature(7, 8), loaded.LastParameters.TimeSignature);
        Assert.Equal(7, loaded.LastParameters.Accents.Count);
        Assert.Equal(AccentLevel.Mute, loaded.LastParameters.Accents[6]);
        Assert.Equal(SubdivisionType.Triplets, loaded.LastParameters.Subdivision);
        Assert.Equal(SoundType.Cowbell, loaded.LastParameters.Sound);
        Assert.Equal(0.75f, loaded.LastParameters.MasterVolume, precision: 3);
        Assert.Equal(0.40f, loaded.LastParameters.SubdivisionVolume, precision: 3);
        Assert.Equal("My Custom 7/8", loaded.SelectedPresetName);
        Assert.Equal(120, loaded.WindowLeft);
        Assert.Equal(80, loaded.WindowTop);
        Assert.Equal(600, loaded.WindowWidth);
        Assert.Equal(780, loaded.WindowHeight);
    }

    [Fact]
    public void SaveAndLoadPresets_RoundTripsCollectionAndSeedsDefaultsWhenMissing()
    {
        var seeded = _storage.LoadPresets(seedDefaultsIfMissing: true);
        Assert.NotEmpty(seeded);

        var customList = new List<MetronomePreset>
        {
            new()
            {
                Name = "Prog 5/8",
                Parameters = new MetronomeParameters
                {
                    Bpm = 175,
                    TimeSignature = new TimeSignature(5, 8),
                    Accents = [AccentLevel.Accent, AccentLevel.Normal, AccentLevel.Normal, AccentLevel.Accent, AccentLevel.Normal],
                    Subdivision = SubdivisionType.Sixteenths,
                    Sound = SoundType.HiHat,
                    MasterVolume = 0.90f,
                    SubdivisionVolume = 0.50f
                }
            }
        };

        _storage.SavePresets(customList);
        var reloaded = _storage.LoadPresets(seedDefaultsIfMissing: false);

        Assert.Single(reloaded);
        Assert.Equal("Prog 5/8", reloaded[0].Name);
        Assert.Equal(175, reloaded[0].Parameters.Bpm);
        Assert.Equal(new TimeSignature(5, 8), reloaded[0].Parameters.TimeSignature);
        Assert.Equal(SubdivisionType.Sixteenths, reloaded[0].Parameters.Subdivision);
        Assert.Equal(SoundType.HiHat, reloaded[0].Parameters.Sound);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

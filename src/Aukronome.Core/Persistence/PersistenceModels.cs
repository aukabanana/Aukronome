using Aukronome.Core.Models;

namespace Aukronome.Core.Persistence;

/// <summary>
/// Named user preset containing a complete snapshot of musical and audio parameters.
/// </summary>
public sealed record MetronomePreset
{
    public string Name { get; init; } = "Untitled";
    public MetronomeParameters Parameters { get; init; } = MetronomeParameters.CreateDefault();

    public MetronomePreset Normalize() => new()
    {
        Name = string.IsNullOrWhiteSpace(Name) ? "Untitled" : Name.Trim(),
        Parameters = (Parameters ?? MetronomeParameters.CreateDefault()).Normalize()
    };
}

/// <summary>
/// Persisted session settings (last-used parameters and window bounds) stored in settings.json.
/// </summary>
public sealed record AppSettings
{
    public MetronomeParameters LastParameters { get; init; } = MetronomeParameters.CreateDefault();
    public string? SelectedPresetName { get; init; }
    public double? WindowLeft { get; init; }
    public double? WindowTop { get; init; }
    public double? WindowWidth { get; init; }
    public double? WindowHeight { get; init; }

    public AppSettings Normalize() => this with
    {
        LastParameters = (LastParameters ?? MetronomeParameters.CreateDefault()).Normalize()
    };
}

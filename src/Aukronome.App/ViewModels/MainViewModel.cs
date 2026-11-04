using System.Collections.ObjectModel;
using Aukronome.Core.Audio;
using Aukronome.Core.Models;
using Aukronome.Core.Persistence;
using Aukronome.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aukronome.App.ViewModels;

public enum PresetEditMode
{
    None,
    SaveAs,
    Rename
}

/// <summary>
/// Primary MVVM ViewModel coordinating the UI, <see cref="MetronomeAudioEngine"/>,
/// <see cref="TapTempoCalculator"/>, and <see cref="StorageService"/>.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly StorageService _storageService;
    private readonly MetronomeAudioEngine _audioEngine;
    private readonly TapTempoCalculator _tapCalculator = new();

    private bool _suppressEngineSync;
    private bool _suppressPresetSelectionLoad;

    public ObservableCollection<BeatDotViewModel> BeatDots { get; } = [];
    public ObservableCollection<MetronomePreset> Presets { get; } = [];

    public IReadOnlyList<OptionItem<SubdivisionType>> SubdivisionOptions { get; } =
        Enum.GetValues<SubdivisionType>()
            .Select(s => new OptionItem<SubdivisionType>(s, s.DisplayName()))
            .ToList();

    public IReadOnlyList<OptionItem<SoundType>> SoundOptions { get; } =
        Enum.GetValues<SoundType>()
            .Select(s => new OptionItem<SoundType>(s, s.DisplayName()))
            .ToList();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TempoMarking))]
    private int _bpm = MetronomeParameters.DefaultBpm;

    [ObservableProperty]
    private string _bpmInputText = MetronomeParameters.DefaultBpm.ToString();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeSignatureDisplay))]
    private int _numerator = 4;

    [ObservableProperty]
    private string _numeratorInputText = "4";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeSignatureDisplay))]
    private int _denominator = 4;

    [ObservableProperty]
    private string _denominatorInputText = "4";

    [ObservableProperty]
    private string _timeSignatureInputText = "4/4";

    public IReadOnlyList<string> QuickSignatures { get; } =
        ["2/4", "3/4", "4/4", "5/4", "6/8", "7/8", "8/8", "9/8", "12/8"];

    [ObservableProperty]
    private SubdivisionType _selectedSubdivision = SubdivisionType.None;

    [ObservableProperty]
    private SoundType _selectedSound = SoundType.Click;

    [ObservableProperty]
    private int _masterVolumePercent = 85;

    [ObservableProperty]
    private int _subdivisionVolumePercent = 55;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayButtonText))]
    private bool _isPlaying;

    [ObservableProperty]
    private int _currentBarDisplay = 1;

    [ObservableProperty]
    private int _currentBeatDisplay;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanModifySelectedPreset))]
    private MetronomePreset? _selectedPreset;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingPresetName))]
    [NotifyPropertyChangedFor(nameof(PresetEditorPrompt))]
    private PresetEditMode _presetEditMode = PresetEditMode.None;

    [ObservableProperty]
    private string _presetNameInput = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Ready — press Space to start";

    public bool IsEditingPresetName => PresetEditMode != PresetEditMode.None;

    public string PresetEditorPrompt => PresetEditMode switch
    {
        PresetEditMode.SaveAs => "Save new preset as:",
        PresetEditMode.Rename => "Rename preset to:",
        _ => string.Empty
    };

    public bool CanModifySelectedPreset => SelectedPreset is not null;

    public string PlayButtonText => IsPlaying ? "STOP  (Space)" : "START  (Space)";

    public string TimeSignatureDisplay => $"{Numerator}/{Denominator}";

    public string TempoMarking => Bpm switch
    {
        < 25 => "Larghissimo",
        < 45 => "Grave",
        < 60 => "Largo",
        < 66 => "Larghetto",
        < 76 => "Adagio",
        < 108 => "Andante",
        < 120 => "Moderato",
        < 156 => "Allegro",
        < 176 => "Vivace",
        < 200 => "Presto",
        _ => "Prestissimo"
    };

    public MainViewModel(StorageService? storageService = null)
    {
        _storageService = storageService ?? new StorageService();
        var settings = _storageService.LoadSettings();
        var loadedPresets = _storageService.LoadPresets(seedDefaultsIfMissing: true);

        foreach (var preset in loadedPresets)
        {
            Presets.Add(preset);
        }

        _audioEngine = new MetronomeAudioEngine(48000, settings.LastParameters);
        ApplyParametersToViewModel(settings.LastParameters);

        if (!string.IsNullOrWhiteSpace(settings.SelectedPresetName))
        {
            _suppressPresetSelectionLoad = true;
            SelectedPreset = Presets.FirstOrDefault(
                p => string.Equals(p.Name, settings.SelectedPresetName, StringComparison.OrdinalIgnoreCase));
            _suppressPresetSelectionLoad = false;
        }
    }

    partial void OnBpmChanged(int value)
    {
        int clamped = Math.Clamp(value, MetronomeParameters.MinBpm, MetronomeParameters.MaxBpm);
        if (clamped != value)
        {
            Bpm = clamped;
            return;
        }

        BpmInputText = clamped.ToString();
        SyncToEngine();
    }

    partial void OnNumeratorChanged(int value)
    {
        int clamped = Math.Clamp(value, TimeSignature.MinNumerator, TimeSignature.MaxNumerator);
        if (clamped != value)
        {
            Numerator = clamped;
            return;
        }

        NumeratorInputText = clamped.ToString();
        TimeSignatureInputText = $"{clamped}/{Denominator}";
        if (!_suppressEngineSync)
        {
            RebuildBeatDots(MetronomeParameters.CreateDefaultAccents(clamped, Denominator));
            SyncToEngine();
            StatusMessage = IsPlaying
                ? $"Time signature {clamped}/{Denominator} queued for next bar"
                : $"Time signature set to {clamped}/{Denominator}";
        }
    }

    partial void OnDenominatorChanged(int value)
    {
        int clamped = Math.Clamp(value, TimeSignature.MinDenominator, TimeSignature.MaxDenominator);
        if (clamped != value)
        {
            Denominator = clamped;
            return;
        }

        DenominatorInputText = clamped.ToString();
        TimeSignatureInputText = $"{Numerator}/{clamped}";
        if (!_suppressEngineSync)
        {
            RebuildBeatDots(MetronomeParameters.CreateDefaultAccents(Numerator, clamped));
            SyncToEngine();
            StatusMessage = IsPlaying
                ? $"Time signature {Numerator}/{clamped} queued for next bar"
                : $"Time signature set to {Numerator}/{clamped}";
        }
    }

    partial void OnSelectedSubdivisionChanged(SubdivisionType value)
    {
        if (!_suppressEngineSync)
        {
            SyncToEngine();
            StatusMessage = IsPlaying
                ? $"Subdivision {value.DisplayName()} queued for next bar"
                : $"Subdivision: {value.DisplayName()}";
        }
    }

    partial void OnSelectedSoundChanged(SoundType value)
    {
        if (!_suppressEngineSync)
        {
            SyncToEngine();
            StatusMessage = $"Sound: {value.DisplayName()}";
        }
    }

    partial void OnMasterVolumePercentChanged(int value)
    {
        int clamped = Math.Clamp(value, 0, 100);
        if (clamped != value)
        {
            MasterVolumePercent = clamped;
            return;
        }
        SyncToEngine();
    }

    partial void OnSubdivisionVolumePercentChanged(int value)
    {
        int clamped = Math.Clamp(value, 0, 100);
        if (clamped != value)
        {
            SubdivisionVolumePercent = clamped;
            return;
        }
        SyncToEngine();
    }

    partial void OnSelectedPresetChanged(MetronomePreset? value)
    {
        if (_suppressPresetSelectionLoad || value is null)
        {
            return;
        }

        ApplyParametersToViewModel(value.Parameters);
        StatusMessage = $"Loaded preset \"{value.Name}\"";
    }

    public void CommitBpmInputText()
    {
        if (int.TryParse(BpmInputText.Trim(), out int parsed))
        {
            Bpm = Math.Clamp(parsed, MetronomeParameters.MinBpm, MetronomeParameters.MaxBpm);
        }
        BpmInputText = Bpm.ToString();
    }

    public void CommitTimeSignatureInputText()
    {
        if (TimeSignature.TryParse(TimeSignatureInputText, out var sig))
        {
            SetTimeSignature(sig.Numerator, sig.Denominator);
        }
        else
        {
            TimeSignatureInputText = $"{Numerator}/{Denominator}";
        }
    }

    public void CommitNumeratorInputText()
    {
        if (TimeSignature.TryParse(NumeratorInputText, out var sig))
        {
            SetTimeSignature(sig.Numerator, sig.Denominator);
            return;
        }

        if (int.TryParse(NumeratorInputText.Trim(), out int parsed))
        {
            Numerator = Math.Clamp(parsed, TimeSignature.MinNumerator, TimeSignature.MaxNumerator);
        }
        NumeratorInputText = Numerator.ToString();
    }

    public void CommitDenominatorInputText()
    {
        if (int.TryParse(DenominatorInputText.Trim(), out int parsed))
        {
            Denominator = Math.Clamp(parsed, TimeSignature.MinDenominator, TimeSignature.MaxDenominator);
        }
        DenominatorInputText = Denominator.ToString();
    }

    [RelayCommand]
    public void SelectTimeSignature(string signature)
    {
        if (TimeSignature.TryParse(signature, out var sig))
        {
            SetTimeSignature(sig.Numerator, sig.Denominator);
        }
    }

    public void SetTimeSignature(int numerator, int denominator)
    {
        int clampedNum = Math.Clamp(numerator, TimeSignature.MinNumerator, TimeSignature.MaxNumerator);
        int clampedDen = Math.Clamp(denominator, TimeSignature.MinDenominator, TimeSignature.MaxDenominator);

        _suppressEngineSync = true;
        try
        {
            Numerator = clampedNum;
            NumeratorInputText = clampedNum.ToString();
            Denominator = clampedDen;
            DenominatorInputText = clampedDen.ToString();
            TimeSignatureInputText = $"{clampedNum}/{clampedDen}";
            RebuildBeatDots(MetronomeParameters.CreateDefaultAccents(clampedNum, clampedDen));
        }
        finally
        {
            _suppressEngineSync = false;
        }

        SyncToEngine();
        StatusMessage = IsPlaying
            ? $"Time signature {clampedNum}/{clampedDen} queued for next bar"
            : $"Time signature set to {clampedNum}/{clampedDen}";
    }

    [RelayCommand]
    public void AdjustBpm(string deltaStr)
    {
        if (int.TryParse(deltaStr, out int delta))
        {
            Bpm = Math.Clamp(Bpm + delta, MetronomeParameters.MinBpm, MetronomeParameters.MaxBpm);
        }
    }

    [RelayCommand]
    private void IncrementNumerator() =>
        Numerator = Math.Clamp(Numerator + 1, TimeSignature.MinNumerator, TimeSignature.MaxNumerator);

    [RelayCommand]
    private void DecrementNumerator() =>
        Numerator = Math.Clamp(Numerator - 1, TimeSignature.MinNumerator, TimeSignature.MaxNumerator);

    private static readonly int[] MusicalDenominators = [1, 2, 4, 8, 16, 32, 64];

    [RelayCommand]
    private void IncrementDenominator()
    {
        for (int i = 0; i < MusicalDenominators.Length; i++)
        {
            if (MusicalDenominators[i] > Denominator)
            {
                Denominator = MusicalDenominators[i];
                return;
            }
        }
    }

    [RelayCommand]
    private void DecrementDenominator()
    {
        for (int i = MusicalDenominators.Length - 1; i >= 0; i--)
        {
            if (MusicalDenominators[i] < Denominator)
            {
                Denominator = MusicalDenominators[i];
                return;
            }
        }
    }

    [RelayCommand]
    private void SetQuickDenominator(string denomStr)
    {
        if (int.TryParse(denomStr, out int denom))
        {
            Denominator = Math.Clamp(denom, TimeSignature.MinDenominator, TimeSignature.MaxDenominator);
        }
    }

    [RelayCommand]
    public void TapTempo()
    {
        int? computed = _tapCalculator.RegisterTap();
        if (computed.HasValue)
        {
            Bpm = computed.Value;
            StatusMessage = $"Tap tempo: {computed.Value} BPM ({_tapCalculator.TapCount} taps)";
        }
        else
        {
            StatusMessage = "Tap again to set tempo...";
        }
    }

    [RelayCommand]
    public void TogglePlay()
    {
        if (IsPlaying)
        {
            _audioEngine.Stop();
            IsPlaying = false;
            ClearBeatHighlights();
            StatusMessage = "Stopped";
        }
        else
        {
            SyncToEngine();
            try
            {
                _audioEngine.Start();
                IsPlaying = true;
                StatusMessage = $"Playing {Bpm} BPM in {Numerator}/{Denominator}";
            }
            catch (Exception ex)
            {
                IsPlaying = false;
                StatusMessage = $"Audio device error: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    public void SavePreset()
    {
        if (SelectedPreset is not null)
        {
            var updated = new MetronomePreset
            {
                Name = SelectedPreset.Name,
                Parameters = BuildCurrentParameters()
            }.Normalize();

            int index = Presets.IndexOf(SelectedPreset);
            if (index >= 0)
            {
                _suppressPresetSelectionLoad = true;
                Presets[index] = updated;
                SelectedPreset = updated;
                _suppressPresetSelectionLoad = false;
                _storageService.SavePresets(Presets);
                StatusMessage = $"Saved preset \"{updated.Name}\"";
                return;
            }
        }

        BeginSaveAsPreset();
    }

    [RelayCommand]
    private void BeginSaveAsPreset()
    {
        PresetNameInput = SelectedPreset is not null
            ? $"{SelectedPreset.Name} Copy"
            : $"{Numerator}/{Denominator} @ {Bpm} BPM";
        PresetEditMode = PresetEditMode.SaveAs;
    }

    [RelayCommand]
    private void BeginRenamePreset()
    {
        if (SelectedPreset is null)
        {
            return;
        }
        PresetNameInput = SelectedPreset.Name;
        PresetEditMode = PresetEditMode.Rename;
    }

    [RelayCommand]
    private void ConfirmPresetName()
    {
        string trimmed = PresetNameInput.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            StatusMessage = "Preset name cannot be empty";
            return;
        }

        if (PresetEditMode == PresetEditMode.SaveAs)
        {
            var newPreset = new MetronomePreset
            {
                Name = trimmed,
                Parameters = BuildCurrentParameters()
            }.Normalize();

            int existingIdx = FindPresetIndexByName(newPreset.Name);
            _suppressPresetSelectionLoad = true;
            if (existingIdx >= 0)
            {
                Presets[existingIdx] = newPreset;
                SelectedPreset = newPreset;
            }
            else
            {
                Presets.Add(newPreset);
                SelectedPreset = newPreset;
            }
            _suppressPresetSelectionLoad = false;

            _storageService.SavePresets(Presets);
            PresetEditMode = PresetEditMode.None;
            StatusMessage = $"Saved preset \"{newPreset.Name}\"";
        }
        else if (PresetEditMode == PresetEditMode.Rename && SelectedPreset is not null)
        {
            int currentIdx = Presets.IndexOf(SelectedPreset);
            if (currentIdx >= 0)
            {
                // Remove any conflicting preset with the target name
                int conflictIdx = FindPresetIndexByName(trimmed);
                if (conflictIdx >= 0 && conflictIdx != currentIdx)
                {
                    Presets.RemoveAt(conflictIdx);
                    currentIdx = Presets.IndexOf(SelectedPreset);
                }

                var renamed = SelectedPreset with { Name = trimmed };
                _suppressPresetSelectionLoad = true;
                Presets[currentIdx] = renamed;
                SelectedPreset = renamed;
                _suppressPresetSelectionLoad = false;

                _storageService.SavePresets(Presets);
                StatusMessage = $"Renamed preset to \"{renamed.Name}\"";
            }
            PresetEditMode = PresetEditMode.None;
        }
    }

    [RelayCommand]
    private void CancelPresetName()
    {
        PresetEditMode = PresetEditMode.None;
    }

    [RelayCommand]
    private void DeletePreset()
    {
        if (SelectedPreset is null)
        {
            return;
        }

        string deletedName = SelectedPreset.Name;
        Presets.Remove(SelectedPreset);
        SelectedPreset = Presets.FirstOrDefault();
        _storageService.SavePresets(Presets);
        StatusMessage = $"Deleted preset \"{deletedName}\"";
    }

    /// <summary>
    /// Called on each WPF frame (CompositionTarget.Rendering) to synchronize the beat dots
    /// with the latency-compensated WASAPI playback position.
    /// </summary>
    public void UpdateVisualSync()
    {
        if (!IsPlaying)
        {
            return;
        }

        HeardBeatState state = _audioEngine.GetHeardBeatState();
        if (!state.IsPlaying || state.BeatIndex < 0)
        {
            return;
        }

        CurrentBarDisplay = state.BarIndex + 1;
        CurrentBeatDisplay = state.BeatIndex + 1;

        double intensity = state.IsPulseLit
            ? Math.Clamp(1.0 - state.BeatPhase * 1.6, 0.25, 1.0)
            : 0.0;

        for (int i = 0; i < BeatDots.Count; i++)
        {
            bool isBeat = i == state.BeatIndex;
            var dot = BeatDots[i];
            dot.IsCurrentBeat = isBeat;
            dot.IsPulseLit = isBeat && state.IsPulseLit;
            dot.PulseIntensity = isBeat ? intensity : 0.0;
        }
    }

    public MetronomeParameters BuildCurrentParameters()
    {
        var accents = BeatDots.Count == Numerator
            ? BeatDots.Select(d => d.AccentLevel).ToArray()
            : MetronomeParameters.CreateDefaultAccents(Numerator);

        return new MetronomeParameters
        {
            Bpm = Bpm,
            TimeSignature = new TimeSignature(Numerator, Denominator),
            Accents = accents,
            Subdivision = SelectedSubdivision,
            Sound = SelectedSound,
            MasterVolume = MasterVolumePercent / 100f,
            SubdivisionVolume = SubdivisionVolumePercent / 100f
        }.Normalize();
    }

    public void SaveSessionSettings(double? left, double? top, double? width, double? height)
    {
        var settings = new AppSettings
        {
            LastParameters = BuildCurrentParameters(),
            SelectedPresetName = SelectedPreset?.Name,
            WindowLeft = left,
            WindowTop = top,
            WindowWidth = width,
            WindowHeight = height
        };
        _storageService.SaveSettings(settings);
    }

    public AppSettings LoadSessionSettings() => _storageService.LoadSettings();

    private void ApplyParametersToViewModel(MetronomeParameters parameters)
    {
        var norm = parameters.Normalize();
        _suppressEngineSync = true;
        try
        {
            Bpm = norm.Bpm;
            BpmInputText = norm.Bpm.ToString();
            Numerator = norm.TimeSignature.Numerator;
            NumeratorInputText = norm.TimeSignature.Numerator.ToString();
            Denominator = norm.TimeSignature.Denominator;
            DenominatorInputText = norm.TimeSignature.Denominator.ToString();
            TimeSignatureInputText = $"{norm.TimeSignature.Numerator}/{norm.TimeSignature.Denominator}";
            SelectedSubdivision = norm.Subdivision;
            SelectedSound = norm.Sound;
            MasterVolumePercent = (int)Math.Round(norm.MasterVolume * 100f);
            SubdivisionVolumePercent = (int)Math.Round(norm.SubdivisionVolume * 100f);
            RebuildBeatDots(norm.Accents);
        }
        finally
        {
            _suppressEngineSync = false;
        }

        SyncToEngine();
    }

    private void RebuildBeatDots(IReadOnlyList<AccentLevel> accents)
    {
        BeatDots.Clear();
        for (int i = 0; i < accents.Count; i++)
        {
            BeatDots.Add(new BeatDotViewModel(i, accents[i], OnBeatDotAccentCycled));
        }
    }

    private void OnBeatDotAccentCycled(BeatDotViewModel dot)
    {
        SyncToEngine();
        StatusMessage = $"Beat {dot.BeatNumber}: {dot.AccentLevel}";
    }

    private void ClearBeatHighlights()
    {
        CurrentBeatDisplay = 0;
        foreach (var dot in BeatDots)
        {
            dot.IsCurrentBeat = false;
            dot.IsPulseLit = false;
            dot.PulseIntensity = 0.0;
        }
    }

    private void SyncToEngine()
    {
        if (_suppressEngineSync)
        {
            return;
        }
        _audioEngine.UpdateParameters(BuildCurrentParameters());
    }

    private int FindPresetIndexByName(string name)
    {
        for (int i = 0; i < Presets.Count; i++)
        {
            if (string.Equals(Presets[i].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    public void Dispose()
    {
        _audioEngine.Dispose();
    }
}

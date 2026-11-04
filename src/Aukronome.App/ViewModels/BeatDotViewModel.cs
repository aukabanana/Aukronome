using Aukronome.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aukronome.App.ViewModels;

/// <summary>
/// ViewModel for a single clickable beat indicator dot in the beat row.
/// </summary>
public partial class BeatDotViewModel : ObservableObject
{
    private readonly Action<BeatDotViewModel> _onCycleAccent;

    public int BeatIndex { get; }
    public int BeatNumber => BeatIndex + 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccentLabel))]
    private AccentLevel _accentLevel;

    [ObservableProperty]
    private bool _isCurrentBeat;

    [ObservableProperty]
    private bool _isPulseLit;

    [ObservableProperty]
    private double _pulseIntensity;

    public string AccentLabel => AccentLevel switch
    {
        AccentLevel.Accent => "ACC",
        AccentLevel.Normal => "NRM",
        AccentLevel.Mute => "MUTE",
        _ => ""
    };

    public BeatDotViewModel(int beatIndex, AccentLevel initialAccent, Action<BeatDotViewModel> onCycleAccent)
    {
        BeatIndex = beatIndex;
        _accentLevel = initialAccent;
        _onCycleAccent = onCycleAccent;
    }

    [RelayCommand]
    private void CycleAccent()
    {
        AccentLevel = AccentLevel.Next();
        _onCycleAccent(this);
    }
}

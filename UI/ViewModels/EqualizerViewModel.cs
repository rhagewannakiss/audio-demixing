using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Services;
using System.Collections.ObjectModel;

namespace AudioStemPlayer.UI.ViewModels;
public partial class EqualizerViewModel : ViewModelBase
{
    private readonly IEqService _eqService;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _selectedPreset;

    [ObservableProperty]
    private ObservableCollection<EqualizerBand> _bands;

    [ObservableProperty]
    private string[] _presets;

    public EqualizerViewModel(IEqService eqService)
    {
        _eqService = eqService;
        Bands = _eqService.Bands;
        Presets = _eqService.Presets;
        IsEnabled = _eqService.IsEnabled;
        SelectedPreset = _eqService.SelectedPreset;

        
        for (int i = 0; i < Bands.Count; i++)
        {
            int capturedIndex = i;
            Bands[i].PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(EqualizerBand.Gain))
                    _eqService.ApplyBand(capturedIndex);
            };
        }
    }

    [RelayCommand]
    private void Toggle()
    {
        IsEnabled = !IsEnabled;
        _eqService.IsEnabled = IsEnabled;
        _eqService.ApplyAll();
    }

    [RelayCommand]
    private void ApplyPreset(string? preset)
    {
        if (preset == null) return;
        SelectedPreset = preset;
        _eqService.ApplyPreset(preset);
    }

    partial void OnIsEnabledChanged(bool value)
    {
        _eqService.IsEnabled = value;
        _eqService.ApplyAll();
    }
    
    partial void OnSelectedPresetChanged(string value)
    {
        ApplyPresetCommand.Execute(value);
    }
}

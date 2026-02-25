using CommunityToolkit.Mvvm.ComponentModel;

namespace AudioStemPlayer.UI.ViewModels;

public partial class PlayerPanelViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _status = "No file loaded";

    [ObservableProperty]
    private double _volume = 50;

    [ObservableProperty]
    private double _position;

    [ObservableProperty]
    private string _currentTime = "0:00";

    [ObservableProperty]
    private string _totalTime = "0:00";
}

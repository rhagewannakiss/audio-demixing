using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;

namespace AudioStemPlayer.UI.ViewModels;

public partial class PlaylistNameDialogViewModel : ViewModelBase
{
    private readonly Window? _window;

    [ObservableProperty]
    private string _playlistName = string.Empty;

    public bool Result { get; private set; }

    public PlaylistNameDialogViewModel(Window window)
    {
        _window = window;
    }

    [RelayCommand]
    private void Ok()
    {
        if (!string.IsNullOrWhiteSpace(PlaylistName))
        {
            Result = true;
            _window?.Close(true);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Result = false;
        _window?.Close(false);
    }
}
using System.Collections.ObjectModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Models;
using Avalonia.Controls;

namespace AudioStemPlayer.UI.ViewModels;

public partial class PlaylistPickerViewModel : ViewModelBase
{
    private readonly Window? _window;

    [ObservableProperty]
    private ObservableCollection<PlaylistInfo> _playlists = [];

    [ObservableProperty]
    private PlaylistInfo? _selectedPlaylist;

    public PlaylistPickerViewModel(Window window, IEnumerable<PlaylistInfo> playlists)
    {
        _window = window;
        Playlists = new ObservableCollection<PlaylistInfo>(playlists);
    }

    [RelayCommand]
    private void Select()
    {
        _window?.Close(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        SelectedPlaylist = null;
        _window?.Close(false);
    }
}
using Avalonia.Controls;
using AudioStemPlayer.Core.Models;
using AudioStemPlayer.UI.ViewModels;
using System.Collections.Generic;

namespace AudioStemPlayer.UI.Views;

public partial class PlaylistPickerDialog : Window
{
    public PlaylistPickerDialog()
    {
        InitializeComponent();
    }

    public PlaylistPickerDialog(IEnumerable<PlaylistInfo> playlists) : this()
    {
        DataContext = new PlaylistPickerViewModel(this, playlists);
    }
}
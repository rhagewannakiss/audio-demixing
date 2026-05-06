using Avalonia.Controls;
using AudioStemPlayer.UI.ViewModels;

namespace AudioStemPlayer.UI.Views;

public partial class PlaylistNameDialog : Window
{
    public PlaylistNameDialog()
    {
        InitializeComponent();
    }

    public PlaylistNameDialog(Window owner) : this()
    {
        DataContext = new PlaylistNameDialogViewModel(this);
    }
}
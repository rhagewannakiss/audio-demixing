using Avalonia.Controls;
using AudioStemPlayer.UI.ViewModels;

namespace AudioStemPlayer.UI.Views;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog()
    {
        InitializeComponent();
    }

    public ConfirmationDialog(string title, string message, bool showCancel = true) : this()
    {
        var vm = new ConfirmationDialogViewModel(this, showCancel)
        {
            Title = title,
            Message = message
        };
        DataContext = vm;
        Title = title;
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;

namespace AudioStemPlayer.UI.ViewModels;

public partial class ConfirmationDialogViewModel : ViewModelBase
{
    private readonly Window? _window;

    public ConfirmationDialogViewModel() {}

    public ConfirmationDialogViewModel(Window window)
    {
        _window = window;
    }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    public bool Result { get; private set; }

    [RelayCommand]
    private void Yes()
    {
        Result = true;
        _window?.Close();
    }

    [RelayCommand]
    private void No()
    {
        Result = false;
        _window?.Close();
    }
}
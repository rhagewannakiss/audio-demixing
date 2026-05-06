using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;

namespace AudioStemPlayer.UI.ViewModels;

public partial class ConfirmationDialogViewModel : ViewModelBase
{
    private readonly Window? _window;
    private readonly bool _showCancel;

    public ConfirmationDialogViewModel() {}

    public ConfirmationDialogViewModel(Window window, bool showCancel = true) : this()
    {
        _window = window;
        _showCancel = showCancel;
        HasCancel = showCancel;
    }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private bool _hasCancel = true;

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

    [RelayCommand]
    private void Ok()
    {
        Result = true;
        _window?.Close();
    }
}
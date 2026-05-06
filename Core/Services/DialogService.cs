using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AudioStemPlayer.UI.Views;
using AudioStemPlayer.UI.ViewModels;
using System.Threading.Tasks;

namespace AudioStemPlayer.Core.Services;

public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null)
            return false;

        var dialog = new ConfirmationDialog(title, message);
        await dialog.ShowDialog<bool>(mainWindow);
        return ((ConfirmationDialogViewModel)dialog.DataContext!).Result;
    }
    
    public async Task ShowErrorAsync(string title, string message)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null)
            return;
        
        var dialog = new ConfirmationDialog(title, message, showCancel: false);
        await dialog.ShowDialog<bool>(mainWindow);
    }

    private static Window? GetMainWindow()
    {
        if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }
}
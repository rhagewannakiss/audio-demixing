using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AudioStemPlayer.UI.Views;
using AudioStemPlayer.UI.ViewModels;
using System.Threading.Tasks;
using AudioStemPlayer.Core.Models;
using System.Collections.Generic;

namespace AudioStemPlayer.Core.Services;

public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message, bool showCancel)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return false;

        var dialog = new ConfirmationDialog(title, message, showCancel);
        await dialog.ShowDialog<bool>(mainWindow);
        return (dialog.DataContext as ConfirmationDialogViewModel)?.Result ?? false;
    }
    
    public async Task ShowErrorAsync(string title, string message)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null)
            return;
        
        var dialog = new ConfirmationDialog(title, message, showCancel: false);
        await dialog.ShowDialog<bool>(mainWindow);
    }

    public async Task<PlaylistInfo?> ShowPlaylistPickerAsync(IEnumerable<PlaylistInfo> playlists)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return null;

        var dialog = new PlaylistPickerDialog(playlists);
        await dialog.ShowDialog<bool>(mainWindow);
        return (dialog.DataContext as PlaylistPickerViewModel)?.SelectedPlaylist;
    }
    
    
    public async Task<string?> ShowPlaylistNameDialogAsync()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return null;
        var dialog = new PlaylistNameDialog(mainWindow);
        await dialog.ShowDialog<bool>(mainWindow);
        var vm = (PlaylistNameDialogViewModel)dialog.DataContext!;
        return vm.Result ? vm.PlaylistName?.Trim() : null;
    }

    private static Window? GetMainWindow()
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }
}
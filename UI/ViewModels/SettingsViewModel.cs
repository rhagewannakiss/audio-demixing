using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace AudioStemPlayer.UI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly PlayerPanelViewModel _playerPanelViewModel;

    public SettingsViewModel(IDialogService dialogService, PlayerPanelViewModel playerPanelViewModel)
    {
        _dialogService = dialogService;
        _playerPanelViewModel = playerPanelViewModel;
    }

    [RelayCommand]
    private async Task ResetApplicationAsync()
    {
        bool confirm = await _dialogService.ShowConfirmationAsync(
            "Reset application",
            "This will delete ALL data (tracks, playlists, history, stems). Are you sure?", true);

        if (!confirm) return;
            
        await _dialogService.ShowConfirmationAsync(
            "Reset application",
            "The app will be restarted", false);
        
        
        _playerPanelViewModel.Unload();

        string appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AudioStemPlayer");

        string dbPath = Path.Combine(appFolder, "library.db");
        string separatedDir = Path.Combine(appFolder, "Separated");

        try
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to delete database: {ex.Message}");
        }

        try
        {
            if (Directory.Exists(separatedDir))
                Directory.Delete(separatedDir, true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to delete stems directory: {ex.Message}");
        }

        string? executablePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(executablePath))
        {
            Process.Start(executablePath);
        }

        Environment.Exit(0);
    }
}
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace AudioStemPlayer.Core.Services;

public class FileService : IFileService
{
    public async Task<string?> OpenFileAsync()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null)
            return null;

        var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Audio File",
            AllowMultiple = false,
            FileTypeFilter = [
                new FilePickerFileType("Audio Files")
                {
                    Patterns = ["*.mp3", "*.wav", "*.flac", "*.ogg"]
                }
            ]
        });

        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<string?> SaveFileAsync(string suggestedFileName, string? initialDirectory = null)
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return null;

        var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save stem as",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "wav",
            FileTypeChoices = [
            
                new FilePickerFileType("WAV audio") { Patterns = [ "*.wav" ] }
            
            ]
        });

        return file?.TryGetLocalPath();
    }

    private static Window? GetMainWindow()
    {
        if (App.Current.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }
}

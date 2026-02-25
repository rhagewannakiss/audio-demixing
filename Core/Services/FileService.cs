using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace AudioStemPlayer.Core.Services;

public class FileService : IFileService
{
    public async Task<string?> OpenFileAsync()
    {
        var mainWindow = App.Current.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (mainWindow == null)
            return null;

        var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Audio File",
            AllowMultiple = false,
            FileTypeFilter = [
                new FilePickerFileType("Audio Files")
                {
                    Patterns = ["*.wav", "*.mp3"] 
                }
            ]
            
        });

        return files.FirstOrDefault()?.TryGetLocalPath();
    }
}

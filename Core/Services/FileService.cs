using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace AudioStemPlayer.Core.Services;

public class FileService : IFileService
{
    private static readonly string[] AudioExtensions = { ".mp3", ".wav", ".flac", ".ogg" };

    public async Task<string?> OpenFileAsync()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null) return null;

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

    public Task<IReadOnlyList<string>> GetAudioFilesFromItemsAsync(IReadOnlyList<IStorageItem> items)
    {
        var paths = new List<string>();
        foreach (var item in items)
        {
            string? localPath = item.TryGetLocalPath();
            if (localPath == null) continue;

            if (item is IStorageFile)
            {
                string extension = Path.GetExtension(localPath);
                if (AudioExtensions.Any(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase)))
                    paths.Add(localPath);
            }
            else if (item is IStorageFolder)
            {
                if (Directory.Exists(localPath))
                {
                    var files = Directory.GetFiles(localPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => AudioExtensions.Any(ext => string.Equals(ext, Path.GetExtension(f), StringComparison.OrdinalIgnoreCase)));
                    paths.AddRange(files);
                }
            }
        }
        return Task.FromResult<IReadOnlyList<string>>(paths);
    }

    private static Window? GetMainWindow()
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace AudioStemPlayer.Core.Services;

public interface IFileService
{
    Task<string?> OpenFileAsync();
    Task<IReadOnlyList<string>> OpenFolderAsync();
    Task<string?> SaveFileAsync(string suggestedFileName, string? initialDirectory = null);
    Task<IReadOnlyList<string>> GetAudioFilesFromItemsAsync(IReadOnlyList<IStorageItem> items);
}

using System.Threading.Tasks;

namespace AudioStemPlayer.Core.Services;

public interface IFileService
{
    Task<string?> OpenFileAsync();
    Task<string?> SaveFileAsync(string suggestedFileName, string? initialDirectory = null);
}
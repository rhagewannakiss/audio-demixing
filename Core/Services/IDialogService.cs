using System.Threading.Tasks;
using AudioStemPlayer.Core.Models;
using System.Collections.Generic;
namespace AudioStemPlayer.Core.Services;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task<PlaylistInfo?> ShowPlaylistPickerAsync(IEnumerable<PlaylistInfo> playlists);
    Task<string?> ShowPlaylistNameDialogAsync();
}
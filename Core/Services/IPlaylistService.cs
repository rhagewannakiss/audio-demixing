using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AudioStemPlayer.Core.Models;

namespace AudioStemPlayer.Core.Services;

public interface IPlaylistService
{
    event EventHandler? PlaylistsChanged;

    Task<IReadOnlyList<PlaylistInfo>> GetPlaylistsAsync(CancellationToken cancellationToken = default);
    Task<PlaylistInfo> CreatePlaylistAsync(string name, CancellationToken cancellationToken = default);
    Task RenamePlaylistAsync(long playlistId, string newName, CancellationToken cancellationToken = default);
    Task DeletePlaylistAsync(long playlistId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrackInfo>> GetPlaylistTracksAsync(long playlistId, CancellationToken cancellationToken = default);
    Task AddTrackToPlaylistAsync(long playlistId, long trackId, CancellationToken cancellationToken = default);
    Task RemoveTrackFromPlaylistAsync(long playlistId, long trackId, CancellationToken cancellationToken = default);
    Task MoveTrackAsync(long playlistId, long trackId, int newPosition, CancellationToken cancellationToken = default);
}

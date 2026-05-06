using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AudioStemPlayer.Core.Models;
using System;

namespace AudioStemPlayer.Core.Services;

public interface ILibraryService
{
    Task<IEnumerable<TrackInfo>> LoadTracksAsync();
    Task AddTrackAsync(TrackInfo track);
    Task RemoveTrackAsync(string filePath);
    Task SaveTracksAsync(IEnumerable<TrackInfo> tracks);
    Task<TrackInfo?> GetTrackByPathAsync(string filePath, CancellationToken cancellationToken = default);
    Task<TrackInfo?> GetTrackByIdAsync(long id, CancellationToken cancellationToken = default);
    event EventHandler? LibraryChanged;
}
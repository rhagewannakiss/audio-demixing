using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AudioStemPlayer.Core.Models;

namespace AudioStemPlayer.Core.Services;

public interface IPlaybackHistoryService
{
    event EventHandler? PlaybackHistoryChanged;

    Task AddPlaybackAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlaybackHistoryInfo>> GetRecentAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

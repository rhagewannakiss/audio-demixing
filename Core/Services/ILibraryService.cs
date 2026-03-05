using System.Collections.Generic;
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
    
    event EventHandler? LibraryChanged; 
}
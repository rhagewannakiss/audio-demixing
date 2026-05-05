namespace AudioStemPlayer.Core.Models;

public class PlaylistTrackInfo
{
    public long PlaylistId { get; set; }
    public long TrackId { get; set; }
    public int Position { get; set; }
    public TrackInfo? Track { get; set; }
}

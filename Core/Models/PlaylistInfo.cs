using System;

namespace AudioStemPlayer.Core.Models;

public class PlaylistInfo
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public int TrackCount { get; set; }
}

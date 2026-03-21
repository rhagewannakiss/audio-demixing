using System;

namespace AudioStemPlayer.Core.Models;

public class TrackInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; } = DateTime.Now;

    public string DisplayName =>
        string.IsNullOrEmpty(Artist) ? Title : $"{Artist} - {Title}";

    public override bool Equals(object? obj) => obj is TrackInfo other && FilePath == other.FilePath;
    public override int GetHashCode() => FilePath?.GetHashCode() ?? 0;
}
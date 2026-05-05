using System;

namespace AudioStemPlayer.Core.Models;

public class TrackInfo
{
    public long Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int? Year { get; set; }
    public double DurationSeconds { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.Now;

    public string DisplayName =>
        string.IsNullOrEmpty(Artist) ? Title : $"{Artist} - {Title}";

    public string DurationText
    {
        get
        {
            if (DurationSeconds <= 0)
                return "--:--";

            var duration = TimeSpan.FromSeconds(DurationSeconds);
            return duration.TotalHours >= 1
                ? duration.ToString(@"h\:mm\:ss")
                : duration.ToString(@"m\:ss");
        }
    }

    public override bool Equals(object? obj) => obj is TrackInfo other && FilePath == other.FilePath;
    public override int GetHashCode() => FilePath?.GetHashCode() ?? 0;
}

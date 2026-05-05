using System;

namespace AudioStemPlayer.Core.Models;

public class PlaybackHistoryInfo
{
    public long Id { get; set; }
    public long? TrackId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public DateTime PlayedAt { get; set; } = DateTime.Now;

    public string DisplayText
    {
        get
        {
            string displayName = string.IsNullOrWhiteSpace(Artist) ? Title : $"{Artist} - {Title}";
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = System.IO.Path.GetFileNameWithoutExtension(FilePath);

            return $"{PlayedAt:g}  |  {displayName}";
        }
    }
}

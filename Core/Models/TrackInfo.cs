namespace AudioStemPlayer.Core.Models;

public class TrackInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;

    public string DisplayName =>
        string.IsNullOrEmpty(Artist) ? Title : $"{Artist} - {Title}";
}
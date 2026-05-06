namespace AudioStemPlayer.Core.Models;

public class StemFileInfo
{
    public long Id { get; set; }
    public long JobId { get; set; }
    public string StemType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public double Volume { get; set; } = 1.0;
    public bool IsMuted { get; set; }
}

public static class StemNames
{
    public static readonly string[] StableOrder = ["vocals", "drums", "bass", "other"];
}

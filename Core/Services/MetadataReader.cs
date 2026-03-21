using System.IO;
using System.Threading.Tasks;
using AudioStemPlayer.Core.Models;
using TagLib;

namespace AudioStemPlayer.Core.Services;

public class MetadataReader : IMetadataReader
{
    public Task<TrackInfo> ReadAsync(string filePath)
    {
        return Task.Run(() =>
        {
            var track = new TrackInfo { FilePath = filePath };
            try
            {
                using var file = TagLib.File.Create(filePath);
                track.Artist = string.Join(", ", file.Tag.Performers);
                track.Title = file.Tag.Title ?? Path.GetFileNameWithoutExtension(filePath);
                track.Album = file.Tag.Album ?? string.Empty;
            }
            catch
            {
                track.Title = Path.GetFileNameWithoutExtension(filePath);
                track.Artist = string.Empty;
                track.Album = string.Empty;
            }
            return track;
        });
    }
}
using System;
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
            var fileInfo = new FileInfo(filePath);
            var track = new TrackInfo
            {
                FilePath = filePath,
                Title = Path.GetFileNameWithoutExtension(filePath),
                DateAdded = DateTime.Now,
                FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0
            };

            try
            {
                using var file = TagLib.File.Create(filePath);
                track.Artist = string.Join(", ", file.Tag.Performers ?? []);
                track.Title = string.IsNullOrWhiteSpace(file.Tag.Title)
                    ? Path.GetFileNameWithoutExtension(filePath)
                    : file.Tag.Title;
                track.Album = file.Tag.Album ?? string.Empty;
                track.Genre = string.Join(", ", file.Tag.Genres ?? []);
                track.Year = file.Tag.Year > 0 ? checked((int)file.Tag.Year) : null;
                track.DurationSeconds = file.Properties.Duration.TotalSeconds;
            }
            catch
            {
                track.Title = Path.GetFileNameWithoutExtension(filePath);
                track.Artist = string.Empty;
                track.Album = string.Empty;
                track.Genre = string.Empty;
                track.Year = null;
                track.DurationSeconds = 0;
            }
            return track;
        });
    }
}

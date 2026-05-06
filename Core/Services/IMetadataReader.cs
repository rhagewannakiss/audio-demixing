using System.Threading.Tasks;
using System.IO;
using AudioStemPlayer.Core.Models;

namespace AudioStemPlayer.Core.Services;

public interface IMetadataReader
{
    Task<TrackInfo> ReadAsync(string filePath);
    Task<Stream?> GetCoverArtAsync(string filePath);   
}
using System.Threading.Tasks;
using AudioStemPlayer.Core.Models;

namespace AudioStemPlayer.Core.Services;

public interface IMetadataReader
{
    Task<TrackInfo> ReadAsync(string filePath);
}
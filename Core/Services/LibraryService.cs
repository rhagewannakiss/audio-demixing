using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AudioStemPlayer.Core.Models;

namespace AudioStemPlayer.Core.Services;

public class JsonLibraryService : ILibraryService
{
    private readonly string _filePath;
    private List<TrackInfo> _tracks = new();
    public event EventHandler? LibraryChanged;
    
    public JsonLibraryService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appData, "AudioStemPlayer");
        Directory.CreateDirectory(appFolder);
        _filePath = Path.Combine(appFolder, "library.json");
    }

    public async Task<IEnumerable<TrackInfo>> LoadTracksAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new List<TrackInfo>();

            string json = await File.ReadAllTextAsync(_filePath);
            _tracks = JsonSerializer.Deserialize<List<TrackInfo>>(json) ?? new List<TrackInfo>();
            return _tracks;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading library: {ex.Message}");
            return new List<TrackInfo>();
        }
    }

    public async Task AddTrackAsync(TrackInfo track)
    {
        _tracks.Add(track);
        await SaveTracksAsync(_tracks);
        LibraryChanged?.Invoke(this, EventArgs.Empty); 
    }

    public Task RemoveTrackAsync(string filePath)
    {
        _tracks.RemoveAll(t => t.FilePath == filePath);
        return SaveTracksAsync(_tracks);
    }

    public async Task SaveTracksAsync(IEnumerable<TrackInfo> tracks)
    {
        try
        {
            string json = JsonSerializer.Serialize(tracks, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
            LibraryChanged?.Invoke(this, EventArgs.Empty); 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving library: {ex.Message}");
        }
    }
}
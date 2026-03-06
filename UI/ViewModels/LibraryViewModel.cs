using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Services;
using AudioStemPlayer.Core.Models;
using System.Linq;

namespace AudioStemPlayer.UI.ViewModels;

public partial class LibraryViewModel : ViewModelBase
{
    private readonly IFileService _fileService;
    private readonly IMetadataReader _metadataReader;
    private readonly ILibraryService _libraryService;
    
    private HashSet<TrackInfo> _allTracks = new();

    [ObservableProperty]
    private TrackInfo? _selectedTrack;

    [ObservableProperty]
    private ObservableCollection<TrackInfo> _tracks = new();
    
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;
    
    public event Action<string>? TrackSelected;

    public LibraryViewModel(IFileService fileService, IMetadataReader metadataReader, ILibraryService libraryService)
    {
        _fileService = fileService;
        _metadataReader = metadataReader;
        _libraryService = libraryService;

        Task.Run(LoadTracksAsync);
    }

    private async Task LoadTracksAsync()
    {
        var tracks = await _libraryService.LoadTracksAsync();
        _allTracks = tracks.ToHashSet();
        
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            Tracks.Clear();
            foreach (var track in tracks)
                Tracks.Add(track);
        });
    }

    partial void OnSelectedTrackChanged(TrackInfo? value)
    {
        if (value != null)
        {
            TrackSelected?.Invoke(value.FilePath);
        }
    }

    [RelayCommand]
    private async Task LoadTrackAsync()
    {
        var path = await _fileService.OpenFileAsync();
        if (!string.IsNullOrEmpty(path))
        {
            var track = await _metadataReader.ReadAsync(path);

            foreach (var t in _allTracks)
            {
                if (t.FilePath == track.FilePath) return;
            }
            
            _allTracks.Add(track);
            UpdateTracksList();
            await _libraryService.AddTrackAsync(track);
            // SelectedTrack = track;
            StatusMessage = $"Added: {track.DisplayName}";
        }
    }
    
    private void UpdateTracksList()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allTracks
            : _allTracks.Where(t => 
                    t.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.Artist.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.Album.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToHashSet();
        Tracks.Clear();
        foreach (var track in filtered)
        {
            Tracks.Add(track);
        }
    }
    
    partial void OnSearchTextChanged(string value)
    {
        UpdateTracksList();
    }
    
    [RelayCommand]
    private void Search()
    {
        UpdateTracksList();
    }
}
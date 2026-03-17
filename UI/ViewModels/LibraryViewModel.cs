using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Services;
using AudioStemPlayer.Core.Models;

namespace AudioStemPlayer.UI.ViewModels;

public partial class LibraryViewModel : LibraryViewModelBase
{
    private readonly IFileService _fileService;
    private readonly IMetadataReader _metadataReader;

    [ObservableProperty]
    private TrackInfo? _selectedTrack;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public event Action<string>? TrackSelected;

    public LibraryViewModel(IFileService fileService, IMetadataReader metadataReader, ILibraryService libraryService)
        : base(libraryService)
    {
        _fileService = fileService;
        _metadataReader = metadataReader;

        _libraryService.LibraryChanged -= OnLibraryChanged;
        _libraryService.LibraryChanged += OnLibraryChangedWithFilter;
    }

    private async void OnLibraryChangedWithFilter(object? sender, EventArgs e)
    {
        await RefreshLibraryAsync();
        UpdateTracksList();
    }

    partial void OnSelectedTrackChanged(TrackInfo? value)
    {
        if (value != null)
            TrackSelected?.Invoke(value.FilePath);
    }

    [RelayCommand]
    private async Task LoadTrackAsync()
    {
        var path = await _fileService.OpenFileAsync();
        if (string.IsNullOrEmpty(path))
            return;

        var track = await _metadataReader.ReadAsync(path);

        if (_allTracks.Any(t => t.FilePath == track.FilePath))
            return;

        _allTracks.Add(track);
        UpdateTracksList();
        await _libraryService.AddTrackAsync(track);
        StatusMessage = $"Added: {track.DisplayName}";
    }

    private void UpdateTracksList()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? (IEnumerable<TrackInfo>)_allTracks
            : _allTracks.Where(t =>
                    t.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.Artist.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.Album.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Tracks.Clear();
            foreach (var track in filtered)
                Tracks.Add(track);
        });
    }

    partial void OnSearchTextChanged(string value) => UpdateTracksList();

    [RelayCommand]
    private void Search() => UpdateTracksList();

    public override void Dispose()
    {
        _libraryService.LibraryChanged -= OnLibraryChangedWithFilter;
        base.Dispose();
    }
}
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Services;
using AudioStemPlayer.Core.Models;

namespace AudioStemPlayer.UI.ViewModels;

public partial class LibraryViewModel : ViewModelBase
{
    private readonly IFileService _fileService;
    private readonly IMetadataReader _metadataReader;
    private readonly ILibraryService _libraryService;

    [ObservableProperty]
    private TrackInfo? _selectedTrack;

    [ObservableProperty]
    private ObservableCollection<TrackInfo> _tracks = new();

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
            Tracks.Add(track);
            await _libraryService.AddTrackAsync(track);
            SelectedTrack = track;
        }
    }
}
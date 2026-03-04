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

    [ObservableProperty]
    private TrackInfo? _selectedTrack;

    [ObservableProperty]
    private ObservableCollection<TrackInfo> _tracks = new();

    public event Action<string>? TrackSelected;

    public LibraryViewModel(IFileService fileService, IMetadataReader metadataReader)
    {
        _fileService = fileService;
        _metadataReader = metadataReader;
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
            SelectedTrack = track;
        }
    }
}
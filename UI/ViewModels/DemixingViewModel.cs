using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Services;
using AudioStemPlayer.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace AudioStemPlayer.UI.ViewModels;

public partial class DemixingViewModel : ViewModelBase
{
    private readonly IFileService _fileService;
    private readonly IDemixingService _demixingService;
    private readonly ILibraryService _libraryService;
    private bool _isLoadingLibrary;

    [ObservableProperty]
    private string? _selectedFilePath;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _outputFiles = new();

    [ObservableProperty]
    private ObservableCollection<TrackInfo> _libraryTracks = new();

    [ObservableProperty]
    private TrackInfo? _selectedLibraryTrack;

    public DemixingViewModel(IFileService fileService, IDemixingService separationService, ILibraryService libraryService)
    {
        _fileService = fileService;
        _demixingService = separationService;
        _libraryService = libraryService;
        
        _libraryService.LibraryChanged += OnLibraryChanged;

        Task.Run(RefreshLibraryAsync);
    }
    
    private async void OnLibraryChanged(object? sender, EventArgs e)
    {
        if (_isLoadingLibrary)
            return;

        await RefreshLibraryAsync();
    }

    partial void OnSelectedLibraryTrackChanged(TrackInfo? value)
    {
        if (value != null)
        {
            SelectedFilePath = value.FilePath;
            StatusMessage = $"Track: {value.DisplayName}";
        }
    }

    [RelayCommand]
    private async Task RefreshLibraryAsync()
    {
        if (_isLoadingLibrary)
            return;

        _isLoadingLibrary = true;
        try
        {
            StatusMessage = "Loading";
            var tracks = await _libraryService.LoadTracksAsync();
            LibraryTracks.Clear();
            foreach (var track in tracks)
            {
                LibraryTracks.Add(track);
            }
            StatusMessage = "Loaded";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error while loading: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DemixAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(SelectedFilePath))
        {
            StatusMessage = "First choose a file";
            return;
        }

        IsProcessing = true;
        StatusMessage = "Started";
        OutputFiles.Clear();

        try
        {
            var progress = new Progress<string>(message => StatusMessage = message);
            var results = await _demixingService.DemixAsync(SelectedFilePath, progress, cancellationToken);

            foreach (var file in results)
            {
                OutputFiles.Add(file);
            }

            StatusMessage = "Finished";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
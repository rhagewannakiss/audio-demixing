using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Services;
using AudioStemPlayer.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.IO;

namespace AudioStemPlayer.UI.ViewModels;

public partial class DemixingViewModel : LibraryViewModelBase
{
    private readonly IFileService _fileService;
    private readonly IDemixingService _demixingService;

    [ObservableProperty]
    private string? _selectedFilePath;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _outputFiles = [];

    public ObservableCollection<TrackInfo> LibraryTracks => Tracks;

    [ObservableProperty]
    private TrackInfo? _selectedTrack;

    public event Action<string>? StemSelected;

    public DemixingViewModel(IFileService fileService, IDemixingService demixingService, ILibraryService libraryService)
        : base(libraryService)
    {
        _fileService = fileService;
        _demixingService = demixingService;
    }

    partial void OnSelectedTrackChanged(TrackInfo? value)
    {
        if (value != null)
        {
            SelectedFilePath = value.FilePath;
            StatusMessage = $"Track: {value.DisplayName}";
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
                OutputFiles.Add(file);

            StatusMessage = "Finished";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Canceled";
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

    [RelayCommand]
    private void PlayStem(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        StemSelected?.Invoke(filePath);
        //StatusMessage = $"Playing: {Path.GetFileName(filePath)}";
    }

    [RelayCommand]
    private async Task SaveStemAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;

        var fileName = Path.GetFileName(filePath);
        var savePath = await _fileService.SaveFileAsync(fileName);
        if (savePath == null) return;

        try
        {
            File.Copy(filePath, savePath, overwrite: true);
            StatusMessage = $"Saved";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }
}
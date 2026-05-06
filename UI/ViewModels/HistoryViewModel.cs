using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Services;
using AudioStemPlayer.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;

namespace AudioStemPlayer.UI.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    private readonly IProcessingHistoryService _historyService;
    private readonly IFileService _fileService;
    private readonly ILibraryService _libraryService;

    [ObservableProperty]
    private ObservableCollection<ProcessingJobInfo> _jobs = [];

    [ObservableProperty]
    private ProcessingJobInfo? _selectedJob;

    [ObservableProperty]
    private ObservableCollection<StemFileInfo> _selectedJobStems = [];

    [ObservableProperty]
    private string _statusMessage = "Loading history...";

    public bool HasSelectedJob => SelectedJob != null;

    public event Action<string>? TrackPlayRequested;

    public HistoryViewModel(IProcessingHistoryService historyService, IFileService fileService,  ILibraryService libraryService)
    {
        _historyService = historyService;
        _libraryService = libraryService;
        _fileService = fileService;
        _ = LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        StatusMessage = "Loading history...";
        try
        {
            var jobs = await _historyService.GetRecentJobsAsync(limit: 200);
            
            foreach (var job in jobs)
            {
                if (job.TrackId.HasValue)
                {
                    var track = await _libraryService.GetTrackByIdAsync(job.TrackId.Value);
                    if (track != null)
                        job.TrackDisplayName = track.DisplayName;
                }
            }
            
            Jobs.Clear();
            foreach (var job in jobs)
                Jobs.Add(job);
            StatusMessage = Jobs.Count > 0 ? $"Showing {Jobs.Count} job(s)" : "No processing history yet";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading history: {ex.Message}";
        }
    }

    partial void OnSelectedJobChanged(ProcessingJobInfo? value)
    {
        OnPropertyChanged(nameof(HasSelectedJob));
        _ = LoadStemsAsync(value);
    }

    private async Task LoadStemsAsync(ProcessingJobInfo? job)
    {
        SelectedJobStems.Clear();
        if (job == null || job.Status != ProcessingStatuses.Succeeded)
            return;

        try
        {
            var stems = await _historyService.GetStemFilesAsync(job.Id);
            foreach (var stem in stems)
                SelectedJobStems.Add(stem);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading stems: {ex.Message}";
        }
    }

    [RelayCommand]
    private void PlayOriginal(ProcessingJobInfo? job)
    {
        if (job == null || string.IsNullOrWhiteSpace(job.InputFilePath))
            return;
        TrackPlayRequested?.Invoke(job.InputFilePath);
    }

    [RelayCommand]
    private void PlayStem(StemFileInfo? stem)
    {
        if (stem == null || string.IsNullOrWhiteSpace(stem.FilePath))
            return;
        TrackPlayRequested?.Invoke(stem.FilePath);
    }

    [RelayCommand]
    private async Task SaveStemAsync(StemFileInfo? stem)
    {
        if (stem == null || string.IsNullOrWhiteSpace(stem.FilePath))
            return;

        var fileName = Path.GetFileName(stem.FilePath);
        var savePath = await _fileService.SaveFileAsync(fileName);
        if (savePath == null)
            return;

        try
        {
            File.Copy(stem.FilePath, savePath, overwrite: true);
            StatusMessage = $"Saved stem '{stem.StemType}' as {Path.GetFileName(savePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving stem: {ex.Message}";
        }
    }
}
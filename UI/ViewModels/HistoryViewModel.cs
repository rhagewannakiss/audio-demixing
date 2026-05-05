using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Services;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AudioStemPlayer.UI.ViewModels;

public partial class HistoryViewModel : ViewModelBase, IDisposable
{
    private readonly IPlaybackHistoryService _playbackHistoryService;

    [ObservableProperty]
    private ObservableCollection<string> _historyItems = new();

    [ObservableProperty]
    private string _statusMessage = "No history yet";

    [ObservableProperty]
    private string _searchText = string.Empty;

    public HistoryViewModel(IPlaybackHistoryService playbackHistoryService)
    {
        _playbackHistoryService = playbackHistoryService;
        _playbackHistoryService.PlaybackHistoryChanged += OnPlaybackHistoryChanged;
        _ = RefreshHistoryAsync();
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        await _playbackHistoryService.ClearAsync();
        await RefreshHistoryAsync();
    }

    [RelayCommand]
    private Task RefreshHistoryAsync() => LoadHistoryAsync();

    private async void OnPlaybackHistoryChanged(object? sender, EventArgs e)
    {
        await RefreshHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            var history = await _playbackHistoryService.GetRecentAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                HistoryItems = new ObservableCollection<string>(history.Select(item => item.DisplayText));
                StatusMessage = HistoryItems.Count == 0
                    ? "No history yet"
                    : $"Played tracks: {HistoryItems.Count}";
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"History error: {ex.Message}";
        }
    }

    public void Dispose()
    {
        _playbackHistoryService.PlaybackHistoryChanged -= OnPlaybackHistoryChanged;
    }
}

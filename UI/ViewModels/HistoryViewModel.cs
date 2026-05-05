using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Models;
using AudioStemPlayer.Core.Services;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AudioStemPlayer.UI.ViewModels;

public partial class HistoryViewModel : ViewModelBase, IDisposable
{
    private readonly IPlaybackHistoryService _playbackHistoryService;
    private readonly List<PlaybackHistoryInfo> _allHistory = new();

    public event Action<string>? PlayAgainRequested;

    [ObservableProperty]
    private ObservableCollection<PlaybackHistoryRowViewModel> _historyItems = new();

    [ObservableProperty]
    private PlaybackHistoryRowViewModel? _selectedHistoryItem;

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

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedHistoryItemChanged(PlaybackHistoryRowViewModel? value)
    {
        PlayAgainCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanUseSelectedHistoryItem))]
    private void PlayAgain()
    {
        if (SelectedHistoryItem == null)
            return;

        if (!File.Exists(SelectedHistoryItem.FilePath))
        {
            StatusMessage = $"File not found: {SelectedHistoryItem.FilePath}";
            return;
        }

        PlayAgainRequested?.Invoke(SelectedHistoryItem.FilePath);
        StatusMessage = $"Playing again: {SelectedHistoryItem.DisplayName}";
    }

    [RelayCommand(CanExecute = nameof(CanUseSelectedHistoryItem))]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedHistoryItem == null)
            return;

        await _playbackHistoryService.DeleteAsync(SelectedHistoryItem.Id);
        SelectedHistoryItem = null;
        await RefreshHistoryAsync();
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        await _playbackHistoryService.ClearAsync();
        SelectedHistoryItem = null;
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
                _allHistory.Clear();
                _allHistory.AddRange(history);
                ApplyFilter();
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"History error: {ex.Message}";
        }
    }

    private bool CanUseSelectedHistoryItem()
    {
        return SelectedHistoryItem != null;
    }

    private void ApplyFilter()
    {
        var query = SearchText.Trim();
        var filteredHistory = string.IsNullOrWhiteSpace(query)
            ? _allHistory
            : _allHistory.Where(item => MatchesSearch(item, query)).ToList();

        var rows = new List<PlaybackHistoryRowViewModel>();
        string? previousGroup = null;

        foreach (var item in filteredHistory.OrderByDescending(item => item.PlayedAt).ThenByDescending(item => item.Id))
        {
            var group = GetGroupName(item.PlayedAt);
            var groupHeader = group == previousGroup ? string.Empty : group;
            rows.Add(new PlaybackHistoryRowViewModel(item, groupHeader));
            previousGroup = group;
        }

        HistoryItems = new ObservableCollection<PlaybackHistoryRowViewModel>(rows);
        StatusMessage = rows.Count == 0
            ? (string.IsNullOrWhiteSpace(query) ? "No history yet" : "No history matches the search")
            : $"Played tracks shown: {rows.Count}";

        if (SelectedHistoryItem != null && rows.All(item => item.Id != SelectedHistoryItem.Id))
            SelectedHistoryItem = null;
    }

    private static bool MatchesSearch(PlaybackHistoryInfo item, string query)
    {
        return item.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
            || item.Artist.Contains(query, StringComparison.OrdinalIgnoreCase)
            || item.FilePath.Contains(query, StringComparison.OrdinalIgnoreCase)
            || item.PlayedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture).Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetGroupName(DateTime playedAt)
    {
        var date = playedAt.Date;
        var today = DateTime.Now.Date;

        if (date == today)
            return "Today";

        if (date == today.AddDays(-1))
            return "Yesterday";

        return "Earlier";
    }

    public void Dispose()
    {
        _playbackHistoryService.PlaybackHistoryChanged -= OnPlaybackHistoryChanged;
    }
}

public sealed class PlaybackHistoryRowViewModel
{
    public PlaybackHistoryRowViewModel(PlaybackHistoryInfo history, string groupHeader)
    {
        Id = history.Id;
        FilePath = history.FilePath;
        Title = string.IsNullOrWhiteSpace(history.Title)
            ? Path.GetFileNameWithoutExtension(history.FilePath)
            : history.Title;
        Artist = history.Artist;
        PlayedAt = history.PlayedAt;
        GroupHeader = groupHeader;
    }

    public long Id { get; }
    public string FilePath { get; }
    public string Title { get; }
    public string Artist { get; }
    public DateTime PlayedAt { get; }
    public string GroupHeader { get; }
    public bool HasGroupHeader => !string.IsNullOrWhiteSpace(GroupHeader);
    public string DateText => PlayedAt.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture);
    public string TimeText => PlayedAt.ToString("HH:mm", CultureInfo.CurrentCulture);
    public string DisplayName => string.IsNullOrWhiteSpace(Artist) ? Title : $"{Artist} - {Title}";
}

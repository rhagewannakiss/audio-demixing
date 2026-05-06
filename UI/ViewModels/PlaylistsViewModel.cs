using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Models;
using AudioStemPlayer.Core.Services;

namespace AudioStemPlayer.UI.ViewModels;

public partial class PlaylistsViewModel : ViewModelBase
{
    private readonly IPlaylistService _playlistService;
    private readonly ILibraryService _libraryService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<PlaylistInfo> _playlists = [];

    [ObservableProperty]
    private PlaylistInfo? _selectedPlaylist;

    [ObservableProperty]
    private ObservableCollection<TrackInfo> _playlistTracks = [];

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool HasSelectedPlaylist => SelectedPlaylist != null;

    public event Action<string>? TrackPlayRequested;

    public PlaylistsViewModel(
        IPlaylistService playlistService,
        ILibraryService libraryService,
        IDialogService dialogService)
    {
        _playlistService = playlistService;
        _libraryService = libraryService;
        _dialogService = dialogService;

        _playlistService.PlaylistsChanged += OnPlaylistsChanged;
        _ = LoadPlaylistsAsync();
    }

    [RelayCommand]
    private async Task LoadPlaylistsAsync()
    {
        StatusMessage = "Loading playlists...";
        try
        {
            long? selectedId = SelectedPlaylist?.Id;
            var playlists = await _playlistService.GetPlaylistsAsync();
            Playlists.Clear();
            foreach (var p in playlists)
                Playlists.Add(p);

            if (selectedId.HasValue)
                SelectedPlaylist = Playlists.FirstOrDefault(p => p.Id == selectedId.Value);
            else
                SelectedPlaylist = null;

            StatusMessage = Playlists.Count > 0 
                ? $"Playlists: {Playlists.Count}" 
                : "No playlists yet.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading playlists: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreatePlaylistAsync()
    {
        var name = await _dialogService.ShowPlaylistNameDialogAsync();
        if (string.IsNullOrWhiteSpace(name)) return;
        try
        {
            await _playlistService.CreatePlaylistAsync(name);
            await LoadPlaylistsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating playlist: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeletePlaylistAsync()
    {
        if (SelectedPlaylist == null) return;
        bool confirm = await _dialogService.ShowConfirmationAsync(
            "Delete Playlist",
            $"Are you sure you want to delete '{SelectedPlaylist.Name}'?");
        if (!confirm) return;

        try
        {
            await _playlistService.DeletePlaylistAsync(SelectedPlaylist.Id);
            SelectedPlaylist = null;
            await LoadPlaylistsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting playlist: {ex.Message}";
        }
    }

    partial void OnSelectedPlaylistChanged(PlaylistInfo? value)
    {
        OnPropertyChanged(nameof(HasSelectedPlaylist));
        _ = LoadPlaylistTracksAsync();
    }

    private async Task LoadPlaylistTracksAsync()
    {
        PlaylistTracks.Clear();
        if (SelectedPlaylist == null) return;
        try
        {
            var tracks = await _playlistService.GetPlaylistTracksAsync(SelectedPlaylist.Id);
            foreach (var t in tracks)
                PlaylistTracks.Add(t);
            StatusMessage = $"{tracks.Count} tracks in '{SelectedPlaylist.Name}'";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading tracks: {ex.Message}";
        }
    }

    [RelayCommand]
    private void PlayTrack(TrackInfo? track)
    {
        if (track != null && !string.IsNullOrEmpty(track.FilePath))
            TrackPlayRequested?.Invoke(track.FilePath);
    }

    [RelayCommand]
    private async Task RemoveTrackFromPlaylistAsync(TrackInfo? track)
    {
        if (SelectedPlaylist == null || track == null) return;
        try
        {
            await _playlistService.RemoveTrackFromPlaylistAsync(SelectedPlaylist.Id, track.Id);
            PlaylistTracks.Remove(track);
            StatusMessage = $"Removed '{track.DisplayName}'";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error removing track: {ex.Message}";
        }
    }

    private async void OnPlaylistsChanged(object? sender, EventArgs e) => await LoadPlaylistsAsync();
}
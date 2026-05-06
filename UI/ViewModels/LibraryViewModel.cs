using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Services;
using AudioStemPlayer.Core.Models;

namespace AudioStemPlayer.UI.ViewModels;

public partial class LibraryViewModel : LibraryViewModelBase
{
    private readonly IFileService _fileService;
    private readonly IMetadataReader _metadataReader;
    private readonly IDialogService _dialogService;
    private readonly IPlaylistService _playlistService;
    private bool _isRestoringSelection;

    [ObservableProperty]
    private TrackInfo? _selectedTrack;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public event Action<string>? TrackSelected;
    
    public event Action<string>? TrackRemoved;

    public LibraryViewModel(
        IFileService fileService,
        IMetadataReader metadataReader,
        ILibraryService libraryService,
        IDialogService dialogService,
        IPlaylistService playlistService)
        : base(libraryService)
    {
        _fileService = fileService;
        _metadataReader = metadataReader;
        _dialogService = dialogService;
        _playlistService = playlistService;
    }

    partial void OnSelectedTrackChanged(TrackInfo? value)
    {
        OnPropertyChanged(nameof(HasSelectedTrack));
        DeleteTrackCommand.NotifyCanExecuteChanged();
        AddToPlaylistCommand.NotifyCanExecuteChanged();
        if (!_isRestoringSelection && value != null)
            TrackSelected?.Invoke(value.FilePath);
    }

    [RelayCommand]
    private async Task LoadTrackAsync()
    {
        var path = await _fileService.OpenFileAsync();
        if (string.IsNullOrEmpty(path))
            return;

        var track = await _metadataReader.ReadAsync(path);
        track.DateAdded = DateTime.Now;

        if (!_allTracks.TryAdd(track.FilePath, track))
            return;

        await _libraryService.AddTrackAsync(track);

        StatusMessage = $"Added: {track.DisplayName}";
    }

    [RelayCommand(CanExecute = nameof(HasSelectedTrack))]
    private async Task DeleteTrackAsync()
    {
        if (SelectedTrack == null) return;

        bool confirm = await _dialogService.ShowConfirmationAsync(
            "Delete Track",
            $"Are you sure you want to delete \"{SelectedTrack.DisplayName}\"?");

        if (!confirm)
        {
            StatusMessage = "Deletion cancelled";
            return;
        }
        
        string removedPath = SelectedTrack.FilePath;
        string displayName = SelectedTrack.DisplayName;
        await _libraryService.RemoveTrackAsync(removedPath);
        StatusMessage = $"Deleted: {displayName}";
        TrackRemoved?.Invoke(removedPath);

        StatusMessage = $"Deleted: {displayName}";
    }

    private bool HasSelectedTrack => SelectedTrack != null;

    protected override void UpdateDisplayedTracks()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allTracks.Values
            : _allTracks.Values.Where(t =>
                    t.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.Artist.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    t.Album.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var sorted = filtered.OrderByDescending(t => t.DateAdded).ToList();
        string? selectedPath = SelectedTrack?.FilePath;

        Tracks.Clear();
        foreach (var track in sorted)
            Tracks.Add(track);
        
        
        
        
        SelectedTrack = null;

        _isRestoringSelection = true;
        if (selectedPath != null && _allTracks.TryGetValue(selectedPath, out var restoredTrack))
        {
            SelectedTrack = restoredTrack;
        }
        _isRestoringSelection = false;
    }

    partial void OnSearchTextChanged(string value) => UpdateDisplayedTracks();






    [RelayCommand(CanExecute = nameof(HasSelectedTrack))]
    private async Task AddToPlaylistAsync()
    {
        if (SelectedTrack == null) return;
        var playlists = await _playlistService.GetPlaylistsAsync();
        if (playlists.Count == 0)
        {
            StatusMessage = "No playlists available. Create one first.";
        }

        var selectedPlaylist = await _dialogService.ShowPlaylistPickerAsync(playlists);
        if (selectedPlaylist == null) return;

        try
        {
            await _playlistService.AddTrackToPlaylistAsync(selectedPlaylist.Id, SelectedTrack.Id);
            StatusMessage = $"Added '{SelectedTrack.DisplayName}' to '{selectedPlaylist.Name}'";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding to playlist: {ex.Message}";
        }
    }
}
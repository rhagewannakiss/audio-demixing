using CommunityToolkit.Mvvm.ComponentModel;
using AudioStemPlayer.Core.Services;
using AudioStemPlayer.Core.Models;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace AudioStemPlayer.UI.ViewModels;

public abstract partial class LibraryViewModelBase : ViewModelBase, IDisposable
{
    protected readonly ILibraryService _libraryService;
    private bool _isLoadingLibrary;
    protected ConcurrentDictionary<string, TrackInfo> _allTracks = new();

    [ObservableProperty]
    private ObservableCollection<TrackInfo> _tracks = [];

    protected LibraryViewModelBase(ILibraryService libraryService)
    {
        _libraryService = libraryService;
        _libraryService.LibraryChanged += OnLibraryChanged;
        _ = RefreshLibraryAsync();
    }

    private async void OnLibraryChanged(object? sender, EventArgs e)
    {
        await RefreshLibraryAsync();
    }

    protected async Task RefreshLibraryAsync()
    {
        if (_isLoadingLibrary) return;
        _isLoadingLibrary = true;
        
        try
        {
            var tracks = await _libraryService.LoadTracksAsync();
            var newDict = new ConcurrentDictionary<string, TrackInfo>(
                tracks.ToDictionary(t => t.FilePath, t => t)
            );
            _allTracks = newDict;

            var sorted = _allTracks.Values.OrderByDescending(t => t.DateAdded).ToList();

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Tracks.Clear();
                foreach (var track in sorted)
                    Tracks.Add(track);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RefreshLibraryAsync error: {ex.Message}");
        }
        finally
        {
            _isLoadingLibrary = false;
        }
    }

    public virtual void Dispose()
    {
        _libraryService.LibraryChanged -= OnLibraryChanged;
    }
}
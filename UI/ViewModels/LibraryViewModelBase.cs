using CommunityToolkit.Mvvm.ComponentModel;
using AudioStemPlayer.Core.Services;
using AudioStemPlayer.Core.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace AudioStemPlayer.UI.ViewModels;

public abstract partial class LibraryViewModelBase : ViewModelBase, IDisposable
{
    protected readonly ILibraryService _libraryService;
    private bool _isLoadingLibrary;
    protected HashSet<TrackInfo> _allTracks = [];

    [ObservableProperty]
    private ObservableCollection<TrackInfo> _tracks = [];

    protected LibraryViewModelBase(ILibraryService libraryService)
    {
        _libraryService = libraryService;
        _libraryService.LibraryChanged += OnLibraryChanged;
        _ = RefreshLibraryAsync();
    }

    protected async void OnLibraryChanged(object? sender, EventArgs e)
    {
        await RefreshLibraryAsync();
    }

    protected async Task RefreshLibraryAsync()
    {
        if (_isLoadingLibrary)
            return;

        _isLoadingLibrary = true;
        try
        {
            var tracks = await _libraryService.LoadTracksAsync();
            _allTracks = tracks.ToHashSet();

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Tracks.Clear();
                foreach (var track in _allTracks)
                    Tracks.Add(track);
            });
        }
        catch (Exception ex) {}
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
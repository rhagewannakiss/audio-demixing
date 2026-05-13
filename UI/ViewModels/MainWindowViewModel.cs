using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;

namespace AudioStemPlayer.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PlayerPanelViewModel _playerPanelViewModel;
    private LibraryViewModel? _cachedLibraryVm;
    private DemixingViewModel? _cachedDemixingVm;
    private HistoryViewModel? _cachedHistoryVm;
    private PlaylistsViewModel? _cachedPlaylistsVm;
    private SettingsViewModel? _cachedSettingsVm;

    [ObservableProperty]
    private PageType _selectedPage;

    [ObservableProperty]
    private ViewModelBase? _currentPageViewModel;

    [ObservableProperty]
    private ObservableCollection<PageType> _pages = new(Enum.GetValues<PageType>());

    public PlayerPanelViewModel PlayerPanelViewModel => _playerPanelViewModel;

    public MainWindowViewModel(IServiceProvider serviceProvider, PlayerPanelViewModel playerPanelViewModel)
    {

        _serviceProvider = serviceProvider;
        _playerPanelViewModel = playerPanelViewModel;

        _playerPanelViewModel.TrackChanged += OnPlayerTrackChanged;

        SelectedPage = PageType.Library;
        UpdateCurrentPage();
    }

    partial void OnSelectedPageChanged(PageType value) => UpdateCurrentPage();

    private void UpdateCurrentPage()
    {
        if (SelectedPage == PageType.Library)
        {
            if (_cachedLibraryVm == null)
            {
                _cachedLibraryVm = _serviceProvider.GetRequiredService<LibraryViewModel>();
                _cachedLibraryVm.TrackSelected += OnLibraryTrackSelected;
                _cachedLibraryVm.TrackRemoved += OnTrackRemoved;
            }
            CurrentPageViewModel = _cachedLibraryVm;
        }
        else if (SelectedPage == PageType.Demixing)
        {
            if (_cachedDemixingVm == null)
            {
                _cachedDemixingVm = _serviceProvider.GetRequiredService<DemixingViewModel>();
                _cachedDemixingVm.StemSelected += path => _playerPanelViewModel.LoadTrack(path);
            }
            CurrentPageViewModel = _cachedDemixingVm;
        }
        else if (SelectedPage == PageType.History)
        {
            if (_cachedHistoryVm == null)
            {
                _cachedHistoryVm = _serviceProvider.GetRequiredService<HistoryViewModel>();
                _cachedHistoryVm.TrackPlayRequested += OnHistoryPlayRequested;
            }
            _cachedHistoryVm.LoadHistoryCommand.Execute(null);
            CurrentPageViewModel = _cachedHistoryVm;
        }
        else if (SelectedPage == PageType.Playlists)
        {
            if (_cachedPlaylistsVm == null)
            {
                _cachedPlaylistsVm = _serviceProvider.GetRequiredService<PlaylistsViewModel>();
                _cachedPlaylistsVm.TrackPlayRequested += OnPlaylistsTrackSelected;
            }
            CurrentPageViewModel = _cachedPlaylistsVm;
        }
        else if (SelectedPage == PageType.Settings)
        {
            if (_cachedSettingsVm == null)
                _cachedSettingsVm = _serviceProvider.GetRequiredService<SettingsViewModel>();
            CurrentPageViewModel = _cachedSettingsVm;
        }
        else
        {
            CurrentPageViewModel = null;
        }
    }

    private void OnLibraryTrackSelected(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;
        if (_cachedLibraryVm != null)
        {
            var allTracks = _cachedLibraryVm.AllTracksSorted;
            var selected = allTracks.FirstOrDefault(t => t.FilePath == filePath);
            if (selected != null)
            {
                int index = allTracks.IndexOf(selected);
                _playerPanelViewModel.SetQueue(allTracks.Select(t => t.FilePath), index);
                return;
            }
        }
        _playerPanelViewModel.LoadTrack(filePath);
    }

    private void OnPlaylistsTrackSelected(string filePath)
    {
        if (_cachedPlaylistsVm != null)
        {
            var tracks = _cachedPlaylistsVm.PlaylistTracks;
            var selected = tracks.FirstOrDefault(t => t.FilePath == filePath);
            if (selected != null)
            {
                int index = tracks.IndexOf(selected);
                if (index >= 0)
                {
                    _playerPanelViewModel.SetQueue(tracks.Select(t => t.FilePath), index);
                    return;
                }
            }
        }
        _playerPanelViewModel.LoadTrack(filePath);
    }

    private void OnHistoryPlayRequested(string filePath)
    {
        _playerPanelViewModel.LoadTrack(filePath);
        if (_cachedLibraryVm != null)
            _cachedLibraryVm.SetSelectedTrackSilently(filePath);
    }

    private void OnTrackRemoved(string removedPath)
    {
        if (_playerPanelViewModel.CurrentFilePath == removedPath)
        {
            _playerPanelViewModel.Unload();
        }
    }

    private void OnPlayerTrackChanged(string filePath)
    {
        if (_cachedLibraryVm != null) _cachedLibraryVm.SetSelectedTrackSilently(filePath);
    }

    public void Dispose()
    {
        if (_cachedLibraryVm != null) _cachedLibraryVm.TrackRemoved -= OnTrackRemoved;
        if (_cachedHistoryVm != null) _cachedHistoryVm.TrackPlayRequested -= OnHistoryPlayRequested;
        if (_cachedPlaylistsVm != null) _cachedPlaylistsVm.TrackPlayRequested -= OnPlaylistsTrackSelected;
        _playerPanelViewModel.TrackChanged -= OnPlayerTrackChanged;
    }
}
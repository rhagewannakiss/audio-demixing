using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AudioStemPlayer.Core.Services;
using TagLib;

namespace AudioStemPlayer.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IFileService _fileService;
    private readonly IAudioPlayerService _audioPlayer;
    private readonly IMetadataReader _metadataReader;
    private readonly ILibraryService _libraryService;
    private LibraryViewModel? _cachedLibraryVm;

    [ObservableProperty]
    private PageType _selectedPage;

    [ObservableProperty]
    private ViewModelBase? _currentPageViewModel;

    [ObservableProperty]
    private ObservableCollection<PageType> _pages = new(Enum.GetValues<PageType>());

    [ObservableProperty]
    private PlayerPanelViewModel _playerPanelViewModel;

    public MainWindowViewModel(IFileService fileService)
    {
        _fileService = fileService;
        _audioPlayer = new AudioPlayerService();
        _metadataReader = new MetadataReader();
        _libraryService = new JsonLibraryService();
        _playerPanelViewModel = new PlayerPanelViewModel(_audioPlayer);
        SelectedPage = PageType.Library;
        UpdateCurrentPage();
    }

    partial void OnSelectedPageChanged(PageType value)
    {
        UpdateCurrentPage();
    }

    private void UpdateCurrentPage()
    {
        if (SelectedPage == PageType.Library)
        {
            if (_cachedLibraryVm == null)
            {
                _cachedLibraryVm = new LibraryViewModel(_fileService, _metadataReader, _libraryService);
                _cachedLibraryVm.TrackSelected += path => _playerPanelViewModel.LoadTrack(path);
            }
            CurrentPageViewModel = _cachedLibraryVm;
        }
        else
        {
            CurrentPageViewModel = null;
        }
    }

    public void Dispose()
    {
        _playerPanelViewModel.Dispose();
        _audioPlayer.Dispose();
    }
}
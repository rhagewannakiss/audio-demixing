using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AudioStemPlayer.Core.Services;

namespace AudioStemPlayer.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IFileService _fileService;
    private readonly AudioPlayerService _audioPlayer;
    private readonly IMetadataReader _metadataReader;

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
            var libraryVm = new LibraryViewModel(_fileService, _metadataReader);
            libraryVm.TrackSelected += path => _playerPanelViewModel.LoadTrack(path);
            CurrentPageViewModel = libraryVm;
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
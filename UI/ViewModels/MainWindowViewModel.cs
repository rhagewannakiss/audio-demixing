using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using AudioStemPlayer.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AudioStemPlayer.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IFileService _fileService;
    private readonly IAudioPlayerService _audioPlayer;
    private readonly IMetadataReader _metadataReader;
    private readonly ILibraryService _libraryService;
    private readonly IServiceProvider _serviceProvider;
    private readonly PlayerPanelViewModel _playerPanelViewModel;
    private LibraryViewModel? _cachedLibraryVm;

    [ObservableProperty]
    private PageType _selectedPage;

    [ObservableProperty]
    private ViewModelBase? _currentPageViewModel;

    [ObservableProperty]
    private ObservableCollection<PageType> _pages = new(Enum.GetValues<PageType>());

    public PlayerPanelViewModel PlayerPanelViewModel => _playerPanelViewModel;

    public MainWindowViewModel(
        IFileService fileService,
        IAudioPlayerService audioPlayer,
        IMetadataReader metadataReader,
        ILibraryService libraryService,
        IServiceProvider serviceProvider,
        PlayerPanelViewModel playerPanelViewModel)
    {
        _fileService = fileService;
        _audioPlayer = audioPlayer;
        _metadataReader = metadataReader;
        _libraryService = libraryService;
        _serviceProvider = serviceProvider;
        _playerPanelViewModel = playerPanelViewModel;

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
                _cachedLibraryVm = _serviceProvider.GetRequiredService<LibraryViewModel>();
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

    }
}
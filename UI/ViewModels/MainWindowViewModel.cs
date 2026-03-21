using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;

namespace AudioStemPlayer.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PlayerPanelViewModel _playerPanelViewModel;
    private LibraryViewModel? _cachedLibraryVm;
    private DemixingViewModel? _cachedDemixingVm;
    private HistoryViewModel? _cachedHistoryVm;

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
            }
            CurrentPageViewModel = _cachedHistoryVm;
        }
        else
        {
            CurrentPageViewModel = null;
        }
    }

    public void Dispose() {}
}
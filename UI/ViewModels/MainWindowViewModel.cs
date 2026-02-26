using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using AudioStemPlayer.Core.Services;
using System;
using System.Collections.ObjectModel;

namespace AudioStemPlayer.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IFileService _fileService;

    [ObservableProperty]
    private PageType _selectedPage;

    [ObservableProperty]
    private ViewModelBase? _currentPageViewModel;

    [ObservableProperty]
    private ObservableCollection<PageType> _pages = new(Enum.GetValues<PageType>());

    [ObservableProperty]
    private PlayerPanelViewModel _playerPanelViewModel;

    public List<PageType> PageTypes { get; } = [ PageType.Library ];

    public MainWindowViewModel(IFileService fileService)
    {
        _fileService = fileService;
        PlayerPanelViewModel = new PlayerPanelViewModel();
        SelectedPage = PageType.Library;

        
        UpdateCurrentPage();
    }

    partial void OnSelectedPageChanged(PageType value)
    {
        UpdateCurrentPage();
    }

    private void UpdateCurrentPage()
    {
        CurrentPageViewModel = SelectedPage switch
        {
            PageType.Library => new LibraryViewModel(_fileService),
            _ => null
        };
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AudioStemPlayer.UI.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<string> _historyItems = new();

    [ObservableProperty]
    private string _statusMessage = "No history yet";

    [ObservableProperty]
    private string _searchText = string.Empty;

    public HistoryViewModel()
    {
    }

    [RelayCommand]
    private void ClearHistory()
    {
        HistoryItems.Clear();
        StatusMessage = "History cleared";
    }
}
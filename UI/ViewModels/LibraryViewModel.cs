using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Services;


namespace AudioStemPlayer.UI.ViewModels;

public partial class LibraryViewModel : ViewModelBase
{
    private readonly IFileService _fileService;

    [ObservableProperty]
    private string? _selectedTrack;

    [ObservableProperty]
    private ObservableCollection<string> _tracks = new();

    public LibraryViewModel(IFileService fileService)
    {
        _fileService = fileService;
    }

    [RelayCommand]
    private async Task LoadTrackAsync()
    {
        var path = await _fileService.OpenFileAsync();
        if (!string.IsNullOrEmpty(path))
        {
            Tracks.Add(path);
        }
    }
}







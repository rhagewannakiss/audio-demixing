using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AudioStemPlayer.UI.Views;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace AudioStemPlayer.UI.ViewModels;
public enum PlaybackMode
{
    Sequential,
    RepeatOne
}

public partial class PlayerPanelViewModel : ViewModelBase, IDisposable
{
    private readonly IAudioPlayerService _audioPlayer;
    private readonly IMetadataReader _metadataReader;
    private readonly IDialogService _dialogService;
    private readonly IEqService _eqService;
    private bool _isUpdatingFromPlayer;

    [ObservableProperty]
    private string _status = "No file loaded";

    [ObservableProperty]
    private double _volume = 75;

    [ObservableProperty]
    private double _position;

    [ObservableProperty]
    private string _currentTime = "0:00";

    [ObservableProperty]
    private string _totalTime = "0:00";

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isLoaded;

    [ObservableProperty]
    private string _trackTitle = string.Empty;

    [ObservableProperty]
    private string _trackArtist = string.Empty;

    [ObservableProperty]
    private Bitmap? _coverBitmap;

    [ObservableProperty]
    private bool _hasCover;

    [ObservableProperty]
    private bool _hasNoCover = true;

    [ObservableProperty]
    private PlaybackMode _currentPlaybackMode = PlaybackMode.Sequential;

    [ObservableProperty]
    private double _loopStart = 0;

    [ObservableProperty]
    private double _loopEnd = 1.0;

    [ObservableProperty]
    private bool _isLoopEnabled;

    public string? CurrentFilePath { get; private set; }

    private List<string> _queue = new();
    private int _currentIndex = -1;

    public bool HasPreviousTrack => _queue.Count > 0 && _currentIndex > 0;
    public bool HasNextTrack => _queue.Count > 0 && _currentIndex < _queue.Count - 1;
    public bool IsNotPlaying => !IsPlaying;

    public event Action<string>? TrackChanged;

    public PlayerPanelViewModel(IAudioPlayerService audioPlayer, IMetadataReader metadataReader, IDialogService dialogService, IEqService eqService)
    {
        _audioPlayer = audioPlayer;
        _metadataReader = metadataReader;
        _dialogService = dialogService;
        _eqService = eqService;
        _eqService.Connect(_audioPlayer);
        _audioPlayer.PositionChanged += OnPositionChanged;
        _audioPlayer.PlaybackEnded += OnPlaybackEnded;
    }

    private void OnPositionChanged(object? sender, double position)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _isUpdatingFromPlayer = true;
            Position = position;
            
            if (IsLoopEnabled && Position >= LoopEnd)
            {
                _audioPlayer.Position = LoopStart;
                Position = LoopStart;
            }

            if (_audioPlayer.Duration > 0)
            {
                var current = TimeSpan.FromSeconds(Position * _audioPlayer.Duration);
                CurrentTime = current.ToString(@"m\:ss");
            }
            _isUpdatingFromPlayer = false;
        });
    }

    [RelayCommand]
    private void ToggleLoop()
    {
        IsLoopEnabled = !IsLoopEnabled;
        if (IsLoopEnabled)
        {
            LoopStart = 0.01;
            LoopEnd = 0.99;

            if (_audioPlayer.IsLoaded && (Position < LoopStart || Position > LoopEnd))
            {
                _audioPlayer.Position = LoopStart;
                Position = LoopStart;
            }
        }
    }
    
    public bool IsRepeatOne => CurrentPlaybackMode == PlaybackMode.RepeatOne;
    public bool IsNotRepeatOne => !IsRepeatOne;

    partial void OnCurrentPlaybackModeChanged(PlaybackMode value)
    {
        OnPropertyChanged(nameof(IsRepeatOne));
        OnPropertyChanged(nameof(IsNotRepeatOne));
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (CurrentPlaybackMode == PlaybackMode.RepeatOne && _currentIndex >= 0 && _currentIndex < _queue.Count)
            {
                _ = LoadAndPlaySingle(_queue[_currentIndex]);
            }
            else
            {
                PlayNext();
            }
        });
    }

    private void PlayNext()
    {
        if (HasNextTrack)
        {
            _currentIndex++;
            _ = LoadAndPlaySingle(_queue[_currentIndex]);
        }
        else
        {
            IsPlaying = false;
            Position = 0;
            CurrentTime = "0:00";
            Status = "Playback ended";
            NotifyNavigationStateChanged();
        }
    }

    partial void OnVolumeChanged(double value) => _audioPlayer.Volume = (int)value;

    partial void OnPositionChanged(double value)
    {
        if (_audioPlayer.IsLoaded && !_isUpdatingFromPlayer)
        {
            _audioPlayer.Position = value;
            if (_audioPlayer.Duration > 0)
                CurrentTime = TimeSpan.FromSeconds(value * _audioPlayer.Duration).ToString(@"m\:ss");
        }
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (IsPlaying) Pause();
        else Play();
    }

    [RelayCommand]
    private void Play()
    {
        if (_audioPlayer.IsLoaded)
        {
            _audioPlayer.Play();
            IsPlaying = true;
            Status = "Playing";
        }
    }

    [RelayCommand]
    private void Pause()
    {
        if (_audioPlayer.IsPlaying)
        {
            _audioPlayer.Pause();
            IsPlaying = false;
            Status = "Paused";
        }
    }

    [RelayCommand]
    private void Stop()
    {
        if (_audioPlayer.IsLoaded)
        {
            _audioPlayer.Stop();
            IsPlaying = false;
            Status = "Stopped";
        }
    }

    [RelayCommand]
    private void Previous()
    {
        if (!HasPreviousTrack) return;
        _currentIndex--;
        _ = LoadAndPlaySingle(_queue[_currentIndex]);
    }

    [RelayCommand]
    private void Next()
    {
        if (!HasNextTrack) return;
        _currentIndex++;
        _ = LoadAndPlaySingle(_queue[_currentIndex]);
    }

    [RelayCommand]
    private void TogglePlaybackMode()
    {
        CurrentPlaybackMode = CurrentPlaybackMode == PlaybackMode.Sequential
            ? PlaybackMode.RepeatOne
            : PlaybackMode.Sequential;
    }

    

    public async void LoadTrack(string path)
    {
        _queue.Clear();
        _currentIndex = -1;
        NotifyNavigationStateChanged();
        ResetLoop();
        await LoadAndPlaySingle(path);
    }

    public void SetQueue(IEnumerable<string> filePaths, int startIndex)
    {
        _queue = filePaths.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        if (_queue.Count == 0) return;
        _currentIndex = Math.Clamp(startIndex, 0, _queue.Count - 1);
        NotifyNavigationStateChanged();
        ResetLoop();
        _ = LoadAndPlaySingle(_queue[_currentIndex]);
    }

    private async Task LoadAndPlaySingle(string path)
    {
        try
        {
            await _audioPlayer.LoadAsync(path);
            IsLoaded = true;
            CurrentFilePath = path;
            Status = $"Loaded: {Path.GetFileName(path)}";
            TotalTime = TimeSpan.FromSeconds(_audioPlayer.Duration).ToString(@"m\:ss");
            Position = 0;
            CurrentTime = "0:00";

            var trackInfo = await _metadataReader.ReadAsync(path);
            TrackTitle = string.IsNullOrWhiteSpace(trackInfo.Title)
                ? Path.GetFileNameWithoutExtension(path)
                : trackInfo.Title;
            TrackArtist = trackInfo.Artist ?? string.Empty;

            Play();
            _ = LoadCoverAsync(path);

            TrackChanged?.Invoke(path);
            NotifyNavigationStateChanged();
        }
        catch (Exception ex)
        {
            Status = $"Error loading file: {ex.Message}";
            NotifyNavigationStateChanged();
            await _dialogService.ShowConfirmationAsync("Playback Error", $"Cannot load file:\n{ex.Message}", false);
        }
    }

    private async Task LoadCoverAsync(string filePath)
    {
        try
        {
            var stream = await _metadataReader.GetCoverArtAsync(filePath);
            if (stream != null)
            {
                using (stream)
                {
                    CoverBitmap = new Bitmap(stream);
                }
            }
            else
            {
                CoverBitmap = null;
            }
        }
        catch
        {
            CoverBitmap = null;
        }
    }

    partial void OnCoverBitmapChanged(Bitmap? value)
    {
        HasCover = value != null;
        HasNoCover = value == null;
    }

    partial void OnIsPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotPlaying));
    }

    public void Unload()
    {
        _audioPlayer.Unload();
        IsLoaded = false;
        IsPlaying = false;
        CurrentFilePath = null;
        Status = "No file loaded";
        Position = 0;
        CurrentTime = "0:00";
        TotalTime = "0:00";
        TrackTitle = string.Empty;
        TrackArtist = string.Empty;
        CoverBitmap = null;
        _queue.Clear();
        _currentIndex = -1;
        ResetLoop();
        NotifyNavigationStateChanged();
    }


    private void ResetLoop()
    {
        LoopStart = 0;
        LoopEnd = 1.0;
        IsLoopEnabled = false;
    }
    
    private EqualizerWindow? _equalizerWindow;

    [RelayCommand]
    private void OpenEqualizer()
    {
        if (_equalizerWindow != null && _equalizerWindow.IsVisible)
        {
            _equalizerWindow.Close();
            _equalizerWindow = null;
            return;
        }

        _equalizerWindow = EqualizerWindow.Create(_eqService);
        _equalizerWindow.Closed += (s, e) => _equalizerWindow = null;
        _equalizerWindow.Show();
    }

    private void NotifyNavigationStateChanged()
    {
        OnPropertyChanged(nameof(HasPreviousTrack));
        OnPropertyChanged(nameof(HasNextTrack));
    }
    
    public void Dispose()
    {
        _eqService.Disconnect();
        _audioPlayer.PositionChanged -= OnPositionChanged;
        _audioPlayer.PlaybackEnded -= OnPlaybackEnded;
    }
}

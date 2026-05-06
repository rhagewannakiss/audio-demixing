using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace AudioStemPlayer.UI.ViewModels;

public partial class PlayerPanelViewModel : ViewModelBase, IDisposable
{
    private readonly IAudioPlayerService _audioPlayer;
    private readonly IMetadataReader _metadataReader;
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

    public string? CurrentFilePath { get; private set; }

    public PlayerPanelViewModel(IAudioPlayerService audioPlayer, IMetadataReader metadataReader)
    {
        _audioPlayer = audioPlayer;
        _metadataReader = metadataReader;
        _audioPlayer.PositionChanged += OnPositionChanged;
        _audioPlayer.PlaybackEnded += OnPlaybackEnded;
    }

    private void OnPositionChanged(object? sender, double position)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _isUpdatingFromPlayer = true;
            Position = position;
            if (_audioPlayer.Duration > 0)
            {
                var current = TimeSpan.FromSeconds(position * _audioPlayer.Duration);
                CurrentTime = current.ToString(@"m\:ss");
            }
            _isUpdatingFromPlayer = false;
        });
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = false;
            Position = 0;
            CurrentTime = "0:00";
            Status = "Playback ended";
        });
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

    public async void LoadTrack(string path)
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
        }
        catch (Exception ex)
        {
            Status = $"Error loading file: {ex.Message}";
        }
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
    }

    public void Dispose()
    {
        _audioPlayer.PositionChanged -= OnPositionChanged;
        _audioPlayer.PlaybackEnded -= OnPlaybackEnded;
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
}
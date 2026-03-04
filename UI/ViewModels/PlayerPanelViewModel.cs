using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AudioStemPlayer.Core.Services;
using System;
using System.IO;
using Avalonia.Threading;

namespace AudioStemPlayer.UI.ViewModels;

public partial class PlayerPanelViewModel : ViewModelBase, IDisposable
{
    private readonly IAudioPlayerService _audioPlayer;
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

    public PlayerPanelViewModel(IAudioPlayerService audioPlayer)
    {
        _audioPlayer = audioPlayer;
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

    partial void OnVolumeChanged(double value)
    {
        _audioPlayer.Volume = (int)value;
    }

    partial void OnPositionChanged(double value)
    {
        if (_audioPlayer.IsLoaded && !_isUpdatingFromPlayer)
        {
            _audioPlayer.Position = value;
            if (_audioPlayer.Duration > 0)
            {
                var current = TimeSpan.FromSeconds(value * _audioPlayer.Duration);
                CurrentTime = current.ToString(@"m\:ss");
            }
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

    public void LoadTrack(string path)
    {
        try
        {
            _audioPlayer.LoadAsync(path);
            IsLoaded = true;
            Status = $"Loaded: {Path.GetFileName(path)}";
            TotalTime = TimeSpan.FromSeconds(_audioPlayer.Duration).ToString(@"m\:ss");
            Position = 0;
            CurrentTime = "0:00";
            Play();
        }
        catch (Exception ex)
        {
            Status = $"Error loading file: {ex.Message}";
        }
    }

    public void Dispose()
    {
        _audioPlayer.PositionChanged -= OnPositionChanged;
        _audioPlayer.PlaybackEnded -= OnPlaybackEnded;
        _audioPlayer.Dispose();
    }
}
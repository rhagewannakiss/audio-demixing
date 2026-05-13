using System;
using System.IO;
using System.Timers;
using ManagedBass;
using System.Threading.Tasks;

namespace AudioStemPlayer.Core.Services;

public class AudioPlayerService : IAudioPlayerService
{
    private static readonly bool BassInitialized;
    private static readonly Errors BassInitializationError;
    private int _stream;
    private readonly Timer _positionTimer;
    private int _volume = 75;

    static AudioPlayerService()
    {
        BassInitialized = Bass.Init();
        BassInitializationError = Bass.LastError;
    }

    public AudioPlayerService()
    {
        _positionTimer = new Timer(10);
        _positionTimer.Elapsed += OnPositionTimerElapsed;
    }
    
    public int Stream => _stream;
    public event EventHandler<int>? StreamCreated; 

    public event EventHandler<double>? PositionChanged;
    public event EventHandler? PlaybackEnded;

    public int Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0, 100);
            if (_stream != 0)
            {
                Bass.ChannelSetAttribute(_stream, ChannelAttribute.Volume, _volume / 100.0);
            }
        }
    }

    public double Position
    {
        get
        {
            if (_stream == 0) return 0;
            long posBytes = Bass.ChannelGetPosition(_stream);
            double posSeconds = Bass.ChannelBytes2Seconds(_stream, posBytes);
            double duration = Duration;
            return duration > 0 ? posSeconds / duration : 0;
        }
        set
        {
            if (_stream == 0) return;
            double newPosSeconds = value * Duration;
            long newPosBytes = Bass.ChannelSeconds2Bytes(_stream, newPosSeconds);
            Bass.ChannelSetPosition(_stream, newPosBytes);
        }
    }

    public double Duration
    {
        get
        {
            if (_stream == 0) return 0;
            long lenBytes = Bass.ChannelGetLength(_stream);
            return Bass.ChannelBytes2Seconds(_stream, lenBytes);
        }
    }

    public bool IsLoaded => _stream != 0;

    public bool IsPlaying
    {
        get
        {
            if (_stream == 0) return false;
            return Bass.ChannelIsActive(_stream) == PlaybackState.Playing;
        }
    }

    public void Load(string filePath)
    {
        EnsureBassInitialized();

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Audio file not found.", filePath);

        Unload();

        _stream = Bass.CreateStream(filePath);
        if (_stream == 0)
        {
            var error = Bass.LastError;
            throw new InvalidOperationException($"Failed to create audio stream. BASS error: {error}");
        }
        
        if (_stream != 0)
            StreamCreated?.Invoke(this, _stream);
    

        Bass.ChannelSetAttribute(_stream, ChannelAttribute.Volume, _volume / 100.0);

        Bass.ChannelSetSync(_stream, SyncFlags.End, 0, (handle, channel, data, user) =>
        {
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
        });
    }

    public async Task LoadAsync(string filePath)
    {
        Load(filePath);
        await Task.CompletedTask;
    }

    public void Play()
    {
        if (!BassInitialized) return;
        if (_stream == 0) return;
        Bass.ChannelPlay(_stream);
        _positionTimer.Start();
    }

    public void Pause()
    {
        if (!BassInitialized) return;
        if (_stream == 0) return;
        Bass.ChannelPause(_stream);
        _positionTimer.Stop();
    }

    public void Stop()
    {
        if (!BassInitialized) return;
        if (_stream == 0) return;
        Bass.ChannelStop(_stream);
        _positionTimer.Stop();
        Bass.ChannelSetPosition(_stream, 0);
    }

    public void Unload()
    {
        _positionTimer.Stop();
        if (BassInitialized && _stream != 0)
        {
            Bass.StreamFree(_stream);
            _stream = 0;
        }
    }

    private void OnPositionTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!BassInitialized) return;
        if (_stream == 0) return;
        double pos = Position;
        PositionChanged?.Invoke(this, pos);
    }

    public void Dispose()
    {
        _positionTimer?.Stop();
        _positionTimer?.Dispose();
        Unload();
    }

    private static void EnsureBassInitialized()
    {
        if (BassInitialized)
            return;

        throw new InvalidOperationException(
            $"Failed to initialize BASS audio engine. BASS error: {BassInitializationError}. " +
            "On Linux, make sure an ALSA/PulseAudio/PipeWire default output device is available.");
    }
}

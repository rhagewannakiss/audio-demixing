using System;
using System.Threading.Tasks;

namespace AudioStemPlayer.Core.Services;

public interface IAudioPlayerService : IDisposable
{
    Task LoadAsync(string filePath);

 
    void Play();


    void Pause();


    void Stop();

    
    void Unload();


    int Volume { get; set; }


    double Position { get; set; }


    double Duration { get; }


    bool IsLoaded { get; }


    bool IsPlaying { get; }


    event EventHandler<double> PositionChanged;


    event EventHandler PlaybackEnded;
    
    int Stream { get; }
}
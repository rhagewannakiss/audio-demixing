using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AudioStemPlayer.Core.Services;

public class DemixingService : IDemixingService
{
    // placeholder
    public async Task<IReadOnlyList<string>> DemixAsync(string inputFile, IProgress<string> progress, CancellationToken cancellationToken = default)
    {
        // some progress imitation
        progress?.Report("Waiting for demucs");
        await Task.Delay(1000, cancellationToken);

        progress?.Report("Working");
        await Task.Delay(2000, cancellationToken);
        
        var stems = new List<string>
        {
            "vocals.wav",
            "drums.wav",
            "bass.wav",
            "other.wav"
        };

        progress?.Report("Saving");
        await Task.Delay(500, cancellationToken);

        return stems;
    }
}
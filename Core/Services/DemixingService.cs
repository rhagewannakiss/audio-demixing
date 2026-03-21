using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AudioStemPlayer.Core.Services;

public class DemixingService : IDemixingService
{
    public async Task<IReadOnlyList<string>> DemixAsync(string inputFile, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var outputDir = Path.Combine(Path.GetDirectoryName(inputFile), "separated");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"-m demucs -o \"{outputDir}\" \"{inputFile}\" -n htdemucs_ft -j 8",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();

        _ = Task.Run(() => ReadOutputAsync(process.StandardOutput, progress, cancellationToken), cancellationToken);
        _ = Task.Run(() => ReadOutputAsync(process.StandardError, progress, cancellationToken), cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
            throw new Exception("Demucs failed");

        return Directory.GetFiles(Path.Combine(outputDir, "htdemucs", Path.GetFileNameWithoutExtension(inputFile)), "*.wav");
    }

    private async Task ReadOutputAsync(StreamReader reader, IProgress<string> progress, CancellationToken token)
    {
        string? line;
        while ((line = await reader.ReadLineAsync(token)) != null && !token.IsCancellationRequested)
        {
            if (line.Contains('%') && line.Contains('|')) progress?.Report(line);
        }
    }
    
    
}
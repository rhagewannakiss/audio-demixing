using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AudioStemPlayer.Core.Services;

public class DemixingService : IDemixingService
{
    private static readonly string _appDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AudioStemPlayer",
        "Separated");

    public async Task<IReadOnlyList<string>> DemixAsync(string inputFile, IProgress<string> progress, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_appDataPath);

        string inputFileName = Path.GetFileNameWithoutExtension(inputFile);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string outputDir = Path.Combine(_appDataPath, $"{inputFileName}_{timestamp}");

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

        string demucsOutputDir = Path.Combine(outputDir, "htdemucs_ft", Path.GetFileNameWithoutExtension(inputFile));
        return Directory.GetFiles(demucsOutputDir, "*.wav");
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
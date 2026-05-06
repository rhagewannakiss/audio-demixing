using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using AudioStemPlayer.Core.Models;

namespace AudioStemPlayer.Core.Services;

public class DemixingService : IDemixingService
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AudioStemPlayer",
        "Separated");

    private readonly IProcessingHistoryService _processingHistoryService;
    private readonly ILibraryService _libraryService;

    public DemixingService(IProcessingHistoryService processingHistoryService, ILibraryService libraryService)
    {
        _processingHistoryService = processingHistoryService ?? throw new ArgumentNullException(nameof(processingHistoryService));
        _libraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
    }

    public async Task<IReadOnlyList<string>> DemixAsync(string inputFile, IProgress<string> progress, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFile);
        if (!File.Exists(inputFile))
            throw new FileNotFoundException("Input audio file was not found.", inputFile);

        Directory.CreateDirectory(AppDataPath);

        string inputFileName = Path.GetFileNameWithoutExtension(inputFile);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string shortGuid = Guid.NewGuid().ToString("N")[..8];
        string outputDir = Path.Combine(AppDataPath, $"{MakeSafePathSegment(inputFileName)}_{timestamp}_{shortGuid}");
        long? trackId = await TryGetTrackIdAsync(inputFile, cancellationToken);
        var job = await _processingHistoryService.CreateJobAsync(trackId, inputFile, ProcessingOperationTypes.Demix, cancellationToken);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = GetPythonExecutable(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.StartInfo.ArgumentList.Add("-m");
        process.StartInfo.ArgumentList.Add("demucs");
        process.StartInfo.ArgumentList.Add("-o");
        process.StartInfo.ArgumentList.Add(outputDir);
        process.StartInfo.ArgumentList.Add(inputFile);
        process.StartInfo.ArgumentList.Add("-n");
        process.StartInfo.ArgumentList.Add("htdemucs_ft");
        process.StartInfo.ArgumentList.Add("-j");
        process.StartInfo.ArgumentList.Add(GetWorkerCount().ToString());

        Task stdoutTask = Task.CompletedTask;
        Task stderrTask = Task.CompletedTask;

        try
        {
            ReportProgress(progress, "Starting Demucs processing.");
            await _processingHistoryService.MarkJobRunningAsync(job.Id, outputDir, cancellationToken);

            if (!process.Start())
                throw new InvalidOperationException("Python/Demucs process did not start.");

            stdoutTask = ReadOutputAsync(process.StandardOutput, progress, cancellationToken);
            stderrTask = ReadOutputAsync(process.StandardError, progress, cancellationToken);

            await process.WaitForExitAsync(cancellationToken);
            await Task.WhenAll(stdoutTask, stderrTask);

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Demucs failed with exit code {process.ExitCode}.");

            var stems = FindStableStemPaths(inputFile, outputDir);
            var stemRows = stems.Select(path => new StemFileInfo
            {
                JobId = job.Id,
                StemType = Path.GetFileNameWithoutExtension(path),
                FilePath = path
            }).ToList();

            await _processingHistoryService.AddStemFilesAsync(job.Id, stemRows, CancellationToken.None);
            await _processingHistoryService.MarkJobSucceededAsync(job.Id, CancellationToken.None);
            ReportProgress(progress, "Demucs processing completed.");
            return stems;
        }
        catch (OperationCanceledException)
        {
            await KillProcessTreeAsync(process);
            await ObserveReaderTasksAsync(stdoutTask, stderrTask);
            await _processingHistoryService.MarkJobCanceledAsync(job.Id, CancellationToken.None);
            ReportProgress(progress, "Demucs processing was canceled.");
            throw;
        }
        catch (Exception ex)
        {
            await KillProcessTreeAsync(process);
            await ObserveReaderTasksAsync(stdoutTask, stderrTask);
            await _processingHistoryService.MarkJobFailedAsync(job.Id, ex.Message, CancellationToken.None);
            ReportProgress(progress, $"Demucs error: {ex.Message}");
            throw;
        }
        finally
        {
            process.Dispose();
        }
    }

    private async Task<long?> TryGetTrackIdAsync(string inputFile, CancellationToken cancellationToken)
    {
        try
        {
            var track = await _libraryService.GetTrackByPathAsync(inputFile, cancellationToken);
            return track?.Id;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static async Task ReadOutputAsync(StreamReader reader, IProgress<string>? progress, CancellationToken token)
    {
        string? line;
        while ((line = await reader.ReadLineAsync(token)) != null && !token.IsCancellationRequested)
        {
            if (!string.IsNullOrWhiteSpace(line))
                ReportProgress(progress, line);
        }
    }

    private static IReadOnlyList<string> FindStableStemPaths(string inputFile, string outputDir)
    {
        string expectedDirectory = Path.Combine(outputDir, "htdemucs_ft", Path.GetFileNameWithoutExtension(inputFile));
        var found = StemNames.StableOrder
            .Select(stem => Directory.EnumerateFiles(outputDir, $"{stem}.wav", SearchOption.AllDirectories).FirstOrDefault())
            .ToList();

        var missing = StemNames.StableOrder
            .Where((_, index) => found[index] is null)
            .Select(stem => $"{stem}.wav")
            .ToList();

        if (missing.Count > 0)
        {
            string message = $"Demucs completed but expected stem files were missing for '{inputFile}'. " +
                $"Expected under '{expectedDirectory}' or another Demucs output directory inside '{outputDir}'. " +
                $"Missing: {string.Join(", ", missing)}.";
            throw new FileNotFoundException(message);
        }

        return found.Select(path => path!).ToList();
    }

    private static string GetPythonExecutable() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "python" : "python3";

    private static int GetWorkerCount() =>
        Math.Clamp(Environment.ProcessorCount / 2, 1, 8);

    private static string MakeSafePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safeChars = value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        string safe = new string(safeChars).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "track" : safe;
    }

    private static async Task KillProcessTreeAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // The process may have exited between the status check and Kill.
        }

        try
        {
            await process.WaitForExitAsync(CancellationToken.None);
        }
        catch
        {
            // WaitForExitAsync can throw if the process never started or was already disposed.
        }
    }

    private static async Task ObserveReaderTasksAsync(params Task[] readerTasks)
    {
        try
        {
            await Task.WhenAll(readerTasks);
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or ObjectDisposedException or InvalidOperationException)
        {
            // These are expected when cancellation closes redirected process streams.
        }
    }

    private static void ReportProgress(IProgress<string>? progress, string message) =>
        progress?.Report(message);
}

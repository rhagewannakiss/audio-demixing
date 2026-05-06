using System;
using System.IO;

namespace AudioStemPlayer.Core.Models;

public class ProcessingJobInfo
{
    public long Id { get; set; }
    public long? TrackId { get; set; }
    public string InputFilePath { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string Status { get; set; } = ProcessingStatuses.Pending;
    public string OutputDirectory { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    public string FileName => string.IsNullOrWhiteSpace(InputFilePath) 
        ? string.Empty 
        : Path.GetFileName(InputFilePath);

    public string TrackDisplayName { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(TrackDisplayName) ? FileName : TrackDisplayName;
}

public static class ProcessingStatuses
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Canceled = "Canceled";
}

public static class ProcessingOperationTypes
{
    public const string Demix = "Demix";
    public const string NoiseReduction = "NoiseReduction";
}

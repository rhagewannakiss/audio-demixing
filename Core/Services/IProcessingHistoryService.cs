using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AudioStemPlayer.Core.Models;

namespace AudioStemPlayer.Core.Services;

public interface IProcessingHistoryService
{
    event EventHandler? HistoryChanged;

    Task<ProcessingJobInfo> CreateJobAsync(long? trackId, string inputFilePath, string operationType, CancellationToken cancellationToken = default);
    Task MarkJobRunningAsync(long jobId, string outputDirectory, CancellationToken cancellationToken = default);
    Task MarkJobSucceededAsync(long jobId, CancellationToken cancellationToken = default);
    Task MarkJobFailedAsync(long jobId, string errorMessage, CancellationToken cancellationToken = default);
    Task MarkJobCanceledAsync(long jobId, CancellationToken cancellationToken = default);
    Task AddStemFilesAsync(long jobId, IEnumerable<StemFileInfo> stems, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcessingJobInfo>> GetRecentJobsAsync(int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StemFileInfo>> GetStemFilesAsync(long jobId, CancellationToken cancellationToken = default);
}

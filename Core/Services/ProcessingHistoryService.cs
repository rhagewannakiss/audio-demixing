using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AudioStemPlayer.Core.Models;
using AudioStemPlayer.Core.Storage;
using Microsoft.Data.Sqlite;

namespace AudioStemPlayer.Core.Services;

public class ProcessingHistoryService : IProcessingHistoryService
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly DatabaseInitializer _databaseInitializer;

    public event EventHandler? HistoryChanged;

    public ProcessingHistoryService()
        : this(new SqliteConnectionFactory())
    {
    }

    public ProcessingHistoryService(SqliteConnectionFactory connectionFactory)
        : this(connectionFactory, new DatabaseInitializer(connectionFactory))
    {
    }

    public ProcessingHistoryService(SqliteConnectionFactory connectionFactory, DatabaseInitializer databaseInitializer)
    {
        _connectionFactory = connectionFactory;
        _databaseInitializer = databaseInitializer;
    }

    public async Task<ProcessingJobInfo> CreateJobAsync(long? trackId, string inputFilePath, string operationType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationType);

        await _databaseInitializer.InitializeAsync(cancellationToken);
        string now = DateTime.Now.ToString("O");
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
INSERT INTO ProcessingJobs (TrackId, InputFilePath, OperationType, Status, CreatedAt)
VALUES ($trackId, $inputFilePath, $operationType, $status, $createdAt)
RETURNING Id, TrackId, InputFilePath, OperationType, Status, OutputDirectory, ErrorMessage, CreatedAt, StartedAt, FinishedAt;
""";
        command.Parameters.AddWithValue("$trackId", trackId.HasValue ? trackId.Value : DBNull.Value);
        command.Parameters.AddWithValue("$inputFilePath", inputFilePath);
        command.Parameters.AddWithValue("$operationType", operationType);
        command.Parameters.AddWithValue("$status", ProcessingStatuses.Pending);
        command.Parameters.AddWithValue("$createdAt", now);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        var job = ReadJob(reader);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
        return job;
    }

    public Task MarkJobRunningAsync(long jobId, string outputDirectory, CancellationToken cancellationToken = default) =>
        UpdateStatusAsync(jobId, ProcessingStatuses.Running, outputDirectory, null, setStarted: true, setFinished: false, cancellationToken);

    public Task MarkJobSucceededAsync(long jobId, CancellationToken cancellationToken = default) =>
        UpdateStatusAsync(jobId, ProcessingStatuses.Succeeded, null, string.Empty, setStarted: false, setFinished: true, cancellationToken);

    public Task MarkJobFailedAsync(long jobId, string errorMessage, CancellationToken cancellationToken = default) =>
        UpdateStatusAsync(jobId, ProcessingStatuses.Failed, null, errorMessage, setStarted: false, setFinished: true, cancellationToken);

    public Task MarkJobCanceledAsync(long jobId, CancellationToken cancellationToken = default) =>
        UpdateStatusAsync(jobId, ProcessingStatuses.Canceled, null, "Canceled by user.", setStarted: false, setFinished: true, cancellationToken);

    public async Task AddStemFilesAsync(long jobId, IEnumerable<StemFileInfo> stems, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stems);

        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var stem in stems)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = """
INSERT INTO StemFiles (JobId, StemType, FilePath, Volume, IsMuted)
VALUES ($jobId, $stemType, $filePath, $volume, $isMuted);
""";
            command.Parameters.AddWithValue("$jobId", jobId);
            command.Parameters.AddWithValue("$stemType", stem.StemType);
            command.Parameters.AddWithValue("$filePath", stem.FilePath);
            command.Parameters.AddWithValue("$volume", stem.Volume);
            command.Parameters.AddWithValue("$isMuted", stem.IsMuted ? 1 : 0);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<IReadOnlyList<ProcessingJobInfo>> GetRecentJobsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT Id, TrackId, InputFilePath, OperationType, Status, OutputDirectory, ErrorMessage, CreatedAt, StartedAt, FinishedAt
FROM ProcessingJobs
ORDER BY CreatedAt DESC, Id DESC
LIMIT $limit;
""";
        command.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 1000));

        var jobs = new List<ProcessingJobInfo>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            jobs.Add(ReadJob(reader));
        return jobs;
    }

    public async Task<IReadOnlyList<StemFileInfo>> GetStemFilesAsync(long jobId, CancellationToken cancellationToken = default)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT Id, JobId, StemType, FilePath, Volume, IsMuted
FROM StemFiles
WHERE JobId = $jobId
ORDER BY CASE StemType
    WHEN 'vocals' THEN 1
    WHEN 'drums' THEN 2
    WHEN 'bass' THEN 3
    WHEN 'other' THEN 4
    ELSE 5
END, Id;
""";
        command.Parameters.AddWithValue("$jobId", jobId);

        var stems = new List<StemFileInfo>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            stems.Add(new StemFileInfo
            {
                Id = reader.GetInt64(0),
                JobId = reader.GetInt64(1),
                StemType = reader.GetString(2),
                FilePath = reader.GetString(3),
                Volume = reader.GetDouble(4),
                IsMuted = reader.GetInt32(5) != 0
            });
        }

        return stems;
    }

    private async Task UpdateStatusAsync(
        long jobId,
        string status,
        string? outputDirectory,
        string? errorMessage,
        bool setStarted,
        bool setFinished,
        CancellationToken cancellationToken)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
UPDATE ProcessingJobs
SET Status = $status,
    OutputDirectory = COALESCE($outputDirectory, OutputDirectory),
    ErrorMessage = COALESCE($errorMessage, ErrorMessage),
    StartedAt = CASE WHEN $setStarted = 1 THEN $now ELSE StartedAt END,
    FinishedAt = CASE WHEN $setFinished = 1 THEN $now ELSE FinishedAt END
WHERE Id = $jobId;
""";
        command.Parameters.AddWithValue("$status", status);
        command.Parameters.AddWithValue("$outputDirectory", outputDirectory is null ? DBNull.Value : outputDirectory);
        command.Parameters.AddWithValue("$errorMessage", errorMessage is null ? DBNull.Value : errorMessage);
        command.Parameters.AddWithValue("$setStarted", setStarted ? 1 : 0);
        command.Parameters.AddWithValue("$setFinished", setFinished ? 1 : 0);
        command.Parameters.AddWithValue("$now", DateTime.Now.ToString("O"));
        command.Parameters.AddWithValue("$jobId", jobId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) > 0)
            HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private static ProcessingJobInfo ReadJob(SqliteDataReader reader)
    {
        return new ProcessingJobInfo
        {
            Id = reader.GetInt64(0),
            TrackId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
            InputFilePath = reader.GetString(2),
            OperationType = reader.GetString(3),
            Status = reader.GetString(4),
            OutputDirectory = reader.GetString(5),
            ErrorMessage = reader.GetString(6),
            CreatedAt = ParseRequiredDate(reader.GetString(7)),
            StartedAt = reader.IsDBNull(8) ? null : ParseRequiredDate(reader.GetString(8)),
            FinishedAt = reader.IsDBNull(9) ? null : ParseRequiredDate(reader.GetString(9))
        };
    }

    private static DateTime ParseRequiredDate(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}

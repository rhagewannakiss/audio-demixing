using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AudioStemPlayer.Core.Models;
using AudioStemPlayer.Core.Storage;
using Microsoft.Data.Sqlite;

namespace AudioStemPlayer.Core.Services;

public class PlaybackHistoryService : IPlaybackHistoryService
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly DatabaseInitializer _databaseInitializer;
    private readonly JsonLibraryService _libraryService;

    public event EventHandler? PlaybackHistoryChanged;

    public PlaybackHistoryService()
        : this(new SqliteConnectionFactory())
    {
    }

    public PlaybackHistoryService(SqliteConnectionFactory connectionFactory)
        : this(connectionFactory, new DatabaseInitializer(connectionFactory), new JsonLibraryService(connectionFactory))
    {
    }

    public PlaybackHistoryService(
        SqliteConnectionFactory connectionFactory,
        DatabaseInitializer databaseInitializer,
        JsonLibraryService libraryService)
    {
        _connectionFactory = connectionFactory;
        _databaseInitializer = databaseInitializer;
        _libraryService = libraryService;
    }

    public async Task AddPlaybackAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        await _databaseInitializer.InitializeAsync(cancellationToken);
        var track = await _libraryService.GetTrackByPathAsync(filePath, cancellationToken);

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
INSERT INTO PlaybackHistory (TrackId, FilePath, Title, Artist, PlayedAt)
VALUES ($trackId, $filePath, $title, $artist, $playedAt);
""";
        command.Parameters.AddWithValue("$trackId", track?.Id is long id ? id : DBNull.Value);
        command.Parameters.AddWithValue("$filePath", filePath);
        command.Parameters.AddWithValue("$title", string.IsNullOrWhiteSpace(track?.Title)
            ? Path.GetFileNameWithoutExtension(filePath)
            : track.Title);
        command.Parameters.AddWithValue("$artist", track?.Artist ?? string.Empty);
        command.Parameters.AddWithValue("$playedAt", DateTime.Now.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
        PlaybackHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<IReadOnlyList<PlaybackHistoryInfo>> GetRecentAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT Id, TrackId, FilePath, Title, Artist, PlayedAt
FROM PlaybackHistory
ORDER BY PlayedAt DESC, Id DESC
LIMIT $limit;
""";
        command.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 1000));

        var history = new List<PlaybackHistoryInfo>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            history.Add(new PlaybackHistoryInfo
            {
                Id = reader.GetInt64(0),
                TrackId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                FilePath = reader.GetString(2),
                Title = reader.GetString(3),
                Artist = reader.GetString(4),
                PlayedAt = DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            });
        }

        return history;
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM PlaybackHistory;";
        await command.ExecuteNonQueryAsync(cancellationToken);
        PlaybackHistoryChanged?.Invoke(this, EventArgs.Empty);
    }
}

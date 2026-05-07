using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AudioStemPlayer.Core.Models;
using AudioStemPlayer.Core.Storage;
using Microsoft.Data.Sqlite;

namespace AudioStemPlayer.Core.Services;

public class JsonLibraryService : ILibraryService
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly DatabaseInitializer _databaseInitializer;
    
    public event EventHandler? LibraryChanged;
    
    public JsonLibraryService()
        : this(new SqliteConnectionFactory())
    {
    }

    public JsonLibraryService(SqliteConnectionFactory connectionFactory)
        : this(connectionFactory, new DatabaseInitializer(connectionFactory))
    {
    }

    public JsonLibraryService(SqliteConnectionFactory connectionFactory, DatabaseInitializer databaseInitializer)
    {
        _connectionFactory = connectionFactory;
        _databaseInitializer = databaseInitializer;
    }

    public async Task<IEnumerable<TrackInfo>> LoadTracksAsync()
    {
        await _databaseInitializer.InitializeAsync();
        await using var connection = await _connectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT Id, FilePath, Artist, Title, Album, Genre, Year, DurationSeconds, FileSizeBytes, DateAdded
FROM Tracks
ORDER BY DateAdded DESC, Id DESC;
""";

        var tracks = new List<TrackInfo>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tracks.Add(ReadTrack(reader));

        return tracks;
    }

    public async Task AddTrackAsync(TrackInfo track)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(track.FilePath);

        await _databaseInitializer.InitializeAsync();
        await using var connection = await _connectionFactory.OpenConnectionAsync();
        var existing = await ReadTrackByPathAsync(connection, track.FilePath);
        if (existing is not null && HasSameStoredValues(existing, track))
        {
            track.Id = existing.Id;
            return;
        }

        track.Id = await UpsertTrackAsync(connection, track);
        LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task RemoveTrackAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        await _databaseInitializer.InitializeAsync();
        await using var connection = await _connectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Tracks WHERE FilePath = $filePath;";
        command.Parameters.AddWithValue("$filePath", filePath);
        int affected = await command.ExecuteNonQueryAsync();

        if (affected > 0)
            LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveTracksAsync(IEnumerable<TrackInfo> tracks)
    {
        ArgumentNullException.ThrowIfNull(tracks);

        await _databaseInitializer.InitializeAsync();
        await using var connection = await _connectionFactory.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        bool changed = false;
        var tracksToSave = tracks
            .Where(t => !string.IsNullOrWhiteSpace(t.FilePath))
            .GroupBy(t => t.FilePath, StringComparer.Ordinal)
            .Select(group => group.Last())
            .ToList();

        foreach (var track in tracksToSave)
        {
            var existing = await ReadTrackByPathAsync(connection, track.FilePath, (SqliteTransaction)transaction);
            if (existing is not null && HasSameStoredValues(existing, track))
            {
                track.Id = existing.Id;
                continue;
            }

            track.Id = await UpsertTrackAsync(connection, track, (SqliteTransaction)transaction);
            changed = true;
        }

        await transaction.CommitAsync();
        if (changed)
            LibraryChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<IReadOnlyList<TrackInfo>> SearchTracksAsync(
        string? query,
        string? genre = null,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT Id, FilePath, Artist, Title, Album, Genre, Year, DurationSeconds, FileSizeBytes, DateAdded
FROM Tracks
WHERE ($query = '' OR Title LIKE $likeQuery COLLATE NOCASE OR Artist LIKE $likeQuery COLLATE NOCASE OR Album LIKE $likeQuery COLLATE NOCASE)
  AND ($genre = '' OR Genre = $genre COLLATE NOCASE)
  AND ($year IS NULL OR Year = $year)
ORDER BY Artist COLLATE NOCASE, Album COLLATE NOCASE, Title COLLATE NOCASE;
""";

        string normalizedQuery = query?.Trim() ?? string.Empty;
        command.Parameters.AddWithValue("$query", normalizedQuery);
        command.Parameters.AddWithValue("$likeQuery", $"%{normalizedQuery}%");
        command.Parameters.AddWithValue("$genre", genre?.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("$year", year.HasValue ? year.Value : DBNull.Value);

        var tracks = new List<TrackInfo>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            tracks.Add(ReadTrack(reader));

        return tracks;
    }

    public async Task<TrackInfo?> GetTrackByPathAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT Id, FilePath, Artist, Title, Album, Genre, Year, DurationSeconds, FileSizeBytes, DateAdded
FROM Tracks
WHERE FilePath = $filePath;
""";
        command.Parameters.AddWithValue("$filePath", filePath);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadTrack(reader) : null;
    }

    public async Task<TrackInfo?> GetTrackByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT Id, FilePath, Artist, Title, Album, Genre, Year, DurationSeconds, FileSizeBytes, DateAdded
FROM Tracks
WHERE Id = $id;
""";
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadTrack(reader) : null;
    }

    public async Task<IReadOnlyList<TrackInfo>> GetTracksByPathsAsync(IReadOnlyList<string> filePaths)
    {
        if (filePaths == null || filePaths.Count == 0)
            return Array.Empty<TrackInfo>();

        await _databaseInitializer.InitializeAsync();

        var parameters = new List<SqliteParameter>();
        var paramNames = new List<string>();
        for (int i = 0; i < filePaths.Count; i++)
        {
            string pName = $"$p{i}";
            paramNames.Add(pName);
            parameters.Add(new SqliteParameter(pName, filePaths[i]));
        }

        string inClause = string.Join(", ", paramNames);
        string sql = $"""
SELECT Id, FilePath, Artist, Title, Album, Genre, Year, DurationSeconds, FileSizeBytes, DateAdded
FROM Tracks
WHERE FilePath IN ({inClause})
ORDER BY DateAdded DESC, Id DESC;
""";

        await using var connection = await _connectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var p in parameters)
            command.Parameters.Add(p);

        var tracks = new List<TrackInfo>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tracks.Add(ReadTrack(reader));
        return tracks;
    }

    private static async Task<TrackInfo?> ReadTrackByPathAsync(
        SqliteConnection connection,
        string filePath,
        SqliteTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
SELECT Id, FilePath, Artist, Title, Album, Genre, Year, DurationSeconds, FileSizeBytes, DateAdded
FROM Tracks
WHERE FilePath = $filePath;
""";
        command.Parameters.AddWithValue("$filePath", filePath);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadTrack(reader) : null;
    }

    internal static TrackInfo ReadTrack(SqliteDataReader reader)
    {
        return new TrackInfo
        {
            Id = reader.GetInt64(0),
            FilePath = reader.GetString(1),
            Artist = reader.GetString(2),
            Title = reader.GetString(3),
            Album = reader.GetString(4),
            Genre = reader.GetString(5),
            Year = reader.IsDBNull(6) ? null : reader.GetInt32(6),
            DurationSeconds = reader.GetDouble(7),
            FileSizeBytes = reader.GetInt64(8),
            DateAdded = DateTime.Parse(reader.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }

    internal static async Task<long> UpsertTrackAsync(SqliteConnection connection, TrackInfo track, SqliteTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
INSERT INTO Tracks (FilePath, Artist, Title, Album, Genre, Year, DurationSeconds, FileSizeBytes, DateAdded)
VALUES ($filePath, $artist, $title, $album, $genre, $year, $durationSeconds, $fileSizeBytes, $dateAdded)
ON CONFLICT(FilePath) DO UPDATE SET
    Artist = excluded.Artist,
    Title = excluded.Title,
    Album = excluded.Album,
    Genre = excluded.Genre,
    Year = excluded.Year,
    DurationSeconds = excluded.DurationSeconds,
    FileSizeBytes = excluded.FileSizeBytes
RETURNING Id;
""";

        command.Parameters.AddWithValue("$filePath", track.FilePath);
        command.Parameters.AddWithValue("$artist", track.Artist ?? string.Empty);
        command.Parameters.AddWithValue("$title", string.IsNullOrWhiteSpace(track.Title)
            ? System.IO.Path.GetFileNameWithoutExtension(track.FilePath)
            : track.Title);
        command.Parameters.AddWithValue("$album", track.Album ?? string.Empty);
        command.Parameters.AddWithValue("$genre", track.Genre ?? string.Empty);
        command.Parameters.AddWithValue("$year", track.Year.HasValue ? track.Year.Value : DBNull.Value);
        command.Parameters.AddWithValue("$durationSeconds", track.DurationSeconds);
        command.Parameters.AddWithValue("$fileSizeBytes", track.FileSizeBytes);
        command.Parameters.AddWithValue("$dateAdded", (track.DateAdded == default ? DateTime.Now : track.DateAdded).ToString("O"));

        object? id = await command.ExecuteScalarAsync();
        return Convert.ToInt64(id, CultureInfo.InvariantCulture);
    }

    private static bool HasSameStoredValues(TrackInfo existing, TrackInfo candidate)
    {
        return string.Equals(existing.Artist, candidate.Artist ?? string.Empty, StringComparison.Ordinal)
            && string.Equals(existing.Title, GetStoredTitle(candidate), StringComparison.Ordinal)
            && string.Equals(existing.Album, candidate.Album ?? string.Empty, StringComparison.Ordinal)
            && string.Equals(existing.Genre, candidate.Genre ?? string.Empty, StringComparison.Ordinal)
            && existing.Year == candidate.Year
            && Math.Abs(existing.DurationSeconds - candidate.DurationSeconds) < 0.001
            && existing.FileSizeBytes == candidate.FileSizeBytes;
    }

    private static string GetStoredTitle(TrackInfo track) =>
        string.IsNullOrWhiteSpace(track.Title)
            ? System.IO.Path.GetFileNameWithoutExtension(track.FilePath)
            : track.Title;
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AudioStemPlayer.Core.Models;
using AudioStemPlayer.Core.Storage;
using Microsoft.Data.Sqlite;

namespace AudioStemPlayer.Core.Services;

public class PlaylistService : IPlaylistService
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly DatabaseInitializer _databaseInitializer;

    public event EventHandler? PlaylistsChanged;

    public PlaylistService()
        : this(new SqliteConnectionFactory())
    {
    }

    public PlaylistService(SqliteConnectionFactory connectionFactory)
        : this(connectionFactory, new DatabaseInitializer(connectionFactory))
    {
    }

    public PlaylistService(SqliteConnectionFactory connectionFactory, DatabaseInitializer databaseInitializer)
    {
        _connectionFactory = connectionFactory;
        _databaseInitializer = databaseInitializer;
    }

    public async Task<IReadOnlyList<PlaylistInfo>> GetPlaylistsAsync(CancellationToken cancellationToken = default)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT p.Id, p.Name, p.CreatedAt, p.UpdatedAt, COUNT(pt.TrackId) AS TrackCount
FROM Playlists p
LEFT JOIN PlaylistTracks pt ON pt.PlaylistId = p.Id
GROUP BY p.Id, p.Name, p.CreatedAt, p.UpdatedAt
ORDER BY p.UpdatedAt DESC, p.Name COLLATE NOCASE;
""";

        var playlists = new List<PlaylistInfo>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            playlists.Add(new PlaylistInfo
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                CreatedAt = ParseDate(reader.GetString(2)),
                UpdatedAt = ParseDate(reader.GetString(3)),
                TrackCount = reader.GetInt32(4)
            });
        }

        return playlists;
    }

    public async Task<PlaylistInfo> CreatePlaylistAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        await _databaseInitializer.InitializeAsync(cancellationToken);
        string now = DateTime.Now.ToString("O");
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
INSERT INTO Playlists (Name, CreatedAt, UpdatedAt)
VALUES ($name, $createdAt, $updatedAt)
RETURNING Id, Name, CreatedAt, UpdatedAt;
""";
        command.Parameters.AddWithValue("$name", name.Trim());
        command.Parameters.AddWithValue("$createdAt", now);
        command.Parameters.AddWithValue("$updatedAt", now);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        var playlist = new PlaylistInfo
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            CreatedAt = ParseDate(reader.GetString(2)),
            UpdatedAt = ParseDate(reader.GetString(3))
        };

        PlaylistsChanged?.Invoke(this, EventArgs.Empty);
        return playlist;
    }

    public async Task RenamePlaylistAsync(long playlistId, string newName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Playlists SET Name = $name, UpdatedAt = $updatedAt WHERE Id = $id;";
        command.Parameters.AddWithValue("$name", newName.Trim());
        command.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
        command.Parameters.AddWithValue("$id", playlistId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) > 0)
            PlaylistsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DeletePlaylistAsync(long playlistId, CancellationToken cancellationToken = default)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Playlists WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", playlistId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) > 0)
            PlaylistsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<IReadOnlyList<TrackInfo>> GetPlaylistTracksAsync(long playlistId, CancellationToken cancellationToken = default)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
SELECT t.Id, t.FilePath, t.Artist, t.Title, t.Album, t.Genre, t.Year, t.DurationSeconds, t.FileSizeBytes, t.DateAdded
FROM PlaylistTracks pt
JOIN Tracks t ON t.Id = pt.TrackId
WHERE pt.PlaylistId = $playlistId
ORDER BY pt.Position, t.Title COLLATE NOCASE;
""";
        command.Parameters.AddWithValue("$playlistId", playlistId);

        var tracks = new List<TrackInfo>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            tracks.Add(JsonLibraryService.ReadTrack(reader));

        return tracks;
    }

    public async Task AddTrackToPlaylistAsync(long playlistId, long trackId, CancellationToken cancellationToken = default)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        int nextPosition = await GetNextPositionAsync(connection, (SqliteTransaction)transaction, playlistId, cancellationToken);
        int affected;
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = """
INSERT OR IGNORE INTO PlaylistTracks (PlaylistId, TrackId, Position)
VALUES ($playlistId, $trackId, $position);
""";
            command.Parameters.AddWithValue("$playlistId", playlistId);
            command.Parameters.AddWithValue("$trackId", trackId);
            command.Parameters.AddWithValue("$position", nextPosition);
            affected = await command.ExecuteNonQueryAsync(cancellationToken);
        }

        if (affected == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        await RecomputePositionsAsync(connection, (SqliteTransaction)transaction, playlistId, cancellationToken);
        await TouchPlaylistAsync(connection, (SqliteTransaction)transaction, playlistId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        PlaylistsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task RemoveTrackFromPlaylistAsync(long playlistId, long trackId, CancellationToken cancellationToken = default)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = "DELETE FROM PlaylistTracks WHERE PlaylistId = $playlistId AND TrackId = $trackId;";
            command.Parameters.AddWithValue("$playlistId", playlistId);
            command.Parameters.AddWithValue("$trackId", trackId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await RecomputePositionsAsync(connection, (SqliteTransaction)transaction, playlistId, cancellationToken);
        await TouchPlaylistAsync(connection, (SqliteTransaction)transaction, playlistId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        PlaylistsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task MoveTrackAsync(long playlistId, long trackId, int newPosition, CancellationToken cancellationToken = default)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var trackIds = await GetOrderedTrackIdsAsync(connection, (SqliteTransaction)transaction, playlistId, cancellationToken);
        if (!trackIds.Remove(trackId))
            return;

        int targetPosition = Math.Clamp(newPosition, 0, trackIds.Count);
        trackIds.Insert(targetPosition, trackId);
        for (int i = 0; i < trackIds.Count; i++)
            await SetPositionAsync(connection, (SqliteTransaction)transaction, playlistId, trackIds[i], i, cancellationToken);

        await TouchPlaylistAsync(connection, (SqliteTransaction)transaction, playlistId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        PlaylistsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static async Task<int> GetNextPositionAsync(SqliteConnection connection, SqliteTransaction transaction, long playlistId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COALESCE(MAX(Position), -1) + 1 FROM PlaylistTracks WHERE PlaylistId = $playlistId;";
        command.Parameters.AddWithValue("$playlistId", playlistId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private static async Task<List<long>> GetOrderedTrackIdsAsync(SqliteConnection connection, SqliteTransaction transaction, long playlistId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT TrackId FROM PlaylistTracks WHERE PlaylistId = $playlistId ORDER BY Position;";
        command.Parameters.AddWithValue("$playlistId", playlistId);

        var ids = new List<long>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            ids.Add(reader.GetInt64(0));
        return ids;
    }

    private static async Task RecomputePositionsAsync(SqliteConnection connection, SqliteTransaction transaction, long playlistId, CancellationToken cancellationToken)
    {
        var ids = await GetOrderedTrackIdsAsync(connection, transaction, playlistId, cancellationToken);
        for (int i = 0; i < ids.Count; i++)
            await SetPositionAsync(connection, transaction, playlistId, ids[i], i, cancellationToken);
    }

    private static async Task SetPositionAsync(SqliteConnection connection, SqliteTransaction transaction, long playlistId, long trackId, int position, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE PlaylistTracks SET Position = $position WHERE PlaylistId = $playlistId AND TrackId = $trackId;";
        command.Parameters.AddWithValue("$position", position);
        command.Parameters.AddWithValue("$playlistId", playlistId);
        command.Parameters.AddWithValue("$trackId", trackId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task TouchPlaylistAsync(SqliteConnection connection, SqliteTransaction transaction, long playlistId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE Playlists SET UpdatedAt = $updatedAt WHERE Id = $playlistId;";
        command.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("O"));
        command.Parameters.AddWithValue("$playlistId", playlistId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DateTime ParseDate(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}

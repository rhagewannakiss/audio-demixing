using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace AudioStemPlayer.Core.Storage;

public sealed class DatabaseInitializer
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private bool _initialized;

    public DatabaseInitializer(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;

        await _initializeLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
                return;

            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = TableSql;
            await command.ExecuteNonQueryAsync(cancellationToken);

            await EnsureTrackColumnsAsync(connection, cancellationToken);

            command.CommandText = IndexSql;
            await command.ExecuteNonQueryAsync(cancellationToken);
            _initialized = true;
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    private static async Task EnsureTrackColumnsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var columns = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(Tracks);";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                columns.Add(reader.GetString(1));
        }

        foreach (var migration in TrackColumnMigrations)
        {
            if (columns.Contains(migration.ColumnName))
                continue;

            await using var command = connection.CreateCommand();
            command.CommandText = migration.Sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static readonly IReadOnlyList<(string ColumnName, string Sql)> TrackColumnMigrations =
    [
        ("Genre", "ALTER TABLE Tracks ADD COLUMN Genre TEXT NOT NULL DEFAULT '';"),
        ("Year", "ALTER TABLE Tracks ADD COLUMN Year INTEGER NULL;"),
        ("DurationSeconds", "ALTER TABLE Tracks ADD COLUMN DurationSeconds REAL NOT NULL DEFAULT 0;"),
        ("FileSizeBytes", "ALTER TABLE Tracks ADD COLUMN FileSizeBytes INTEGER NOT NULL DEFAULT 0;")
    ];

    private const string TableSql = """
CREATE TABLE IF NOT EXISTS Tracks (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FilePath TEXT NOT NULL UNIQUE,
    Artist TEXT NOT NULL DEFAULT '',
    Title TEXT NOT NULL DEFAULT '',
    Album TEXT NOT NULL DEFAULT '',
    Genre TEXT NOT NULL DEFAULT '',
    Year INTEGER NULL,
    DurationSeconds REAL NOT NULL DEFAULT 0,
    FileSizeBytes INTEGER NOT NULL DEFAULT 0,
    DateAdded TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Playlists (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS PlaylistTracks (
    PlaylistId INTEGER NOT NULL,
    TrackId INTEGER NOT NULL,
    Position INTEGER NOT NULL,
    PRIMARY KEY (PlaylistId, TrackId),
    FOREIGN KEY (PlaylistId) REFERENCES Playlists(Id) ON DELETE CASCADE,
    FOREIGN KEY (TrackId) REFERENCES Tracks(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ProcessingJobs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TrackId INTEGER NULL,
    InputFilePath TEXT NOT NULL,
    OperationType TEXT NOT NULL,
    Status TEXT NOT NULL,
    OutputDirectory TEXT NOT NULL DEFAULT '',
    ErrorMessage TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL,
    StartedAt TEXT NULL,
    FinishedAt TEXT NULL,
    FOREIGN KEY (TrackId) REFERENCES Tracks(Id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS StemFiles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    JobId INTEGER NOT NULL,
    StemType TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    Volume REAL NOT NULL DEFAULT 1.0,
    IsMuted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (JobId) REFERENCES ProcessingJobs(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS PlaybackHistory (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TrackId INTEGER NULL,
    FilePath TEXT NOT NULL,
    Title TEXT NOT NULL DEFAULT '',
    Artist TEXT NOT NULL DEFAULT '',
    PlayedAt TEXT NOT NULL,
    FOREIGN KEY (TrackId) REFERENCES Tracks(Id) ON DELETE SET NULL
);
""";

    private const string IndexSql = """
CREATE UNIQUE INDEX IF NOT EXISTS UX_Tracks_FilePath ON Tracks(FilePath);
CREATE INDEX IF NOT EXISTS IX_Tracks_Title ON Tracks(Title);
CREATE INDEX IF NOT EXISTS IX_Tracks_Artist ON Tracks(Artist);
CREATE INDEX IF NOT EXISTS IX_Tracks_Album ON Tracks(Album);
CREATE INDEX IF NOT EXISTS IX_Tracks_Genre ON Tracks(Genre);
CREATE INDEX IF NOT EXISTS IX_Tracks_Year ON Tracks(Year);
CREATE INDEX IF NOT EXISTS IX_PlaylistTracks_Playlist_Position ON PlaylistTracks(PlaylistId, Position);
CREATE INDEX IF NOT EXISTS IX_ProcessingJobs_CreatedAt ON ProcessingJobs(CreatedAt);
CREATE INDEX IF NOT EXISTS IX_StemFiles_JobId ON StemFiles(JobId);
CREATE INDEX IF NOT EXISTS IX_PlaybackHistory_PlayedAt ON PlaybackHistory(PlayedAt);
""";
}

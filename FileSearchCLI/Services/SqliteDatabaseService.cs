using Microsoft.Data.Sqlite;

namespace FileSearchCLI.Services;

public sealed class SqliteDatabaseService
{
    private const int CurrentSchemaVersion = 1;

    private readonly string _databasePath;
    private readonly string _connectionString;

    public SqliteDatabaseService(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("A database path is required.", nameof(databasePath));
        }

        _databasePath = Path.GetFullPath(databasePath);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true
        }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        var schemaVersion = await GetSchemaVersionAsync(connection, cancellationToken);
        if (schemaVersion > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Database schema version {schemaVersion} is newer than the supported version {CurrentSchemaVersion}.");
        }

        if (schemaVersion < CurrentSchemaVersion)
        {
            await CreateSchemaAsync(connection, cancellationToken);
        }
    }

    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var directoryPath = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var connection = new SqliteConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public bool RemoveDatabase()
    {
        var databaseExists = File.Exists(_databasePath);

        File.Delete(_databasePath + "-wal");
        File.Delete(_databasePath + "-shm");
        File.Delete(_databasePath);

        return databaseExists;
    }

    private async Task<int> GetSchemaVersionAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is not long schemaVersion)
        {
            throw new InvalidOperationException("Could not read the database schema version.");
        }

        return checked((int)schemaVersion);
    }

    private async Task CreateSchemaAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        using var transaction = connection.BeginTransaction();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IndexedFolders (
                Id INTEGER PRIMARY KEY,
                ParentFolderId INTEGER NULL REFERENCES IndexedFolders(Id) ON DELETE CASCADE,
                Name TEXT NOT NULL,
                FullPath TEXT NOT NULL COLLATE NOCASE UNIQUE,
                LastScannedUtcTicks INTEGER NULL CHECK (LastScannedUtcTicks IS NULL OR LastScannedUtcTicks >= 0)
            );

            CREATE INDEX IX_IndexedFolders_ParentFolderId
                ON IndexedFolders (ParentFolderId);

            CREATE TABLE IndexedFiles (
                Id INTEGER PRIMARY KEY,
                FolderId INTEGER NOT NULL REFERENCES IndexedFolders(Id) ON DELETE CASCADE,
                Name TEXT NOT NULL COLLATE NOCASE,
                Extension TEXT NOT NULL COLLATE NOCASE,
                SizeBytes INTEGER NOT NULL CHECK (SizeBytes >= 0),
                LastModifiedUtcTicks INTEGER NOT NULL CHECK (LastModifiedUtcTicks >= 0),
                UNIQUE (FolderId, Name)
            );

            CREATE INDEX IX_IndexedFiles_Name
                ON IndexedFiles (Name);

            CREATE INDEX IX_IndexedFiles_Extension_Name
                ON IndexedFiles (Extension, Name);

            PRAGMA user_version = 1;
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
        transaction.Commit();
    }
}
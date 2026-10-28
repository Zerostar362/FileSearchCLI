using System.IO;
using System.Runtime.CompilerServices;
using FileSearchCLI.Models;
using Microsoft.Data.Sqlite;

namespace FileSearchCLI.Services;

public sealed class IndexerService
{
    private readonly SqliteDatabaseService _databaseService;
    private readonly StringComparer _pathComparer;
    private readonly List<string> _excludedDirectoryPaths = new();

    public IndexerService(SqliteDatabaseService databaseService)
    {
        _databaseService = databaseService;
        _pathComparer = Path.DirectorySeparatorChar == '\\'
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        ConfigureExcludedDirectories();
    }

    public async IAsyncEnumerable<IndexingEvent> IndexAllDrivesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _databaseService.InitializeAsync(cancellationToken);
        await using var connection = await _databaseService.OpenConnectionAsync(cancellationToken);

        foreach (var drive in DriveInfo.GetDrives())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryGetDriveRoot(drive, out var rootPath, out var driveError))
            {
                if (driveError is not null)
                    yield return new IndexingEvent(driveError);

                continue;
            }

            if (IsExcludedDirectory(rootPath))
                continue;

            await foreach (var indexingEvent in ScanDriveAsync(connection, rootPath, cancellationToken))
                yield return indexingEvent;
        }
    }

    private bool TryGetDriveRoot(DriveInfo drive, out string rootPath, out IndexingError? error)
    {
        try
        {
            rootPath = drive.RootDirectory.FullName;
            if (!drive.IsReady)
            {
                error = new IndexingError(rootPath, "Drive is not ready.");
                return false;
            }

            error = null;
            return true;
        }
        catch (IOException exception)
        {
            rootPath = drive.Name;
            error = new IndexingError(rootPath, exception.Message);
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            rootPath = drive.Name;
            error = new IndexingError(rootPath, exception.Message);
            return false;
        }
    }

    private async IAsyncEnumerable<IndexingEvent> ScanDriveAsync(
        SqliteConnection connection,
        string rootPath,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var foldersToScan = new Queue<(string Path, long? ParentFolderId)>();
        foldersToScan.Enqueue((rootPath, null));

        while (foldersToScan.TryDequeue(out var folderToScan))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var errors = new List<IndexingError>();
            var scan = await IndexFolderAsync(
                connection,
                folderToScan.Path,
                folderToScan.ParentFolderId,
                errors,
                cancellationToken);

            foreach (var file in scan.IndexedFiles)
                yield return new IndexingEvent(file);

            foreach (var error in errors)
                yield return new IndexingEvent(error);

            foreach (var childDirectory in scan.ChildDirectories)
            {
                foldersToScan.Enqueue((childDirectory, scan.FolderId));
            }
        }
    }

    private async Task<FolderScanResult> IndexFolderAsync(
        SqliteConnection connection,
        string folderPath,
        long? parentFolderId,
        List<IndexingError> errors,
        CancellationToken cancellationToken)
    {
        var childDirectories = GetChildDirectories(folderPath, errors, out var directoriesComplete);
        var indexedFiles = GetIndexedFiles(folderPath, errors, out var filesComplete);
        var folder = CreateIndexedFolder(folderPath, parentFolderId);
        var isComplete = directoriesComplete && filesComplete;

        var folderId = await SaveFolderAsync(
            connection,
            folder,
            indexedFiles,
            isComplete,
            cancellationToken);

        return new FolderScanResult(folderId, childDirectories, indexedFiles);
    }

    private List<string> GetChildDirectories(
        string folderPath,
        List<IndexingError> errors,
        out bool isComplete)
    {
        var childDirectories = new List<string>();
        var directoryPaths = GetDirectoryPaths(folderPath, errors, out isComplete);

        foreach (var directoryPath in directoryPaths)
        {
            if (IsExcludedDirectory(directoryPath))
                continue;

            if (!TryGetAttributes(directoryPath, errors, out var attributes))
            {
                isComplete = false;
                continue;
            }

            if ((attributes & FileAttributes.ReparsePoint) == 0)
                childDirectories.Add(directoryPath);
        }

        return childDirectories;
    }

    private List<IndexedFile> GetIndexedFiles(
        string folderPath,
        List<IndexingError> errors,
        out bool isComplete)
    {
        var indexedFiles = new List<IndexedFile>();
        var filePaths = GetFilePaths(folderPath, errors, out isComplete);

        foreach (var filePath in filePaths)
        {
            if (!TryCreateIndexedFile(filePath, errors, out var indexedFile, out var isReparsePoint))
            {
                isComplete = false;
                continue;
            }

            if (!isReparsePoint && indexedFile is not null)
                indexedFiles.Add(indexedFile);
        }

        return indexedFiles;
    }

    private string[] GetDirectoryPaths(string folderPath, List<IndexingError> errors, out bool isComplete)
    {
        try
        {
            isComplete = true;
            return Directory.GetDirectories(folderPath);
        }
        catch (IOException exception)
        {
            errors.Add(new IndexingError(folderPath, exception.Message));
        }
        catch (UnauthorizedAccessException exception)
        {
            errors.Add(new IndexingError(folderPath, exception.Message));
        }

        isComplete = false;
        return [];
    }

    private string[] GetFilePaths(string folderPath, List<IndexingError> errors, out bool isComplete)
    {
        try
        {
            isComplete = true;
            return Directory.GetFiles(folderPath);
        }
        catch (IOException exception)
        {
            errors.Add(new IndexingError(folderPath, exception.Message));
        }
        catch (UnauthorizedAccessException exception)
        {
            errors.Add(new IndexingError(folderPath, exception.Message));
        }

        isComplete = false;
        return [];
    }

    private IndexedFolder CreateIndexedFolder(string folderPath, long? parentFolderId)
    {
        var directoryInfo = new DirectoryInfo(folderPath);
        return new IndexedFolder
        {
            ParentFolderId = parentFolderId,
            Name = string.IsNullOrEmpty(directoryInfo.Name) ? folderPath : directoryInfo.Name,
            FullPath = folderPath
        };
    }

    private async Task<long> SaveFolderAsync(
        SqliteConnection connection,
        IndexedFolder folder,
        IReadOnlyList<IndexedFile> indexedFiles,
        bool isComplete,
        CancellationToken cancellationToken)
    {
        using var transaction = connection.BeginTransaction();
        var folderId = await UpsertFolderAsync(connection, transaction, folder, cancellationToken);
        await UpsertFilesAsync(connection, transaction, folderId, indexedFiles, cancellationToken);

        if (isComplete)
            await UpdateFolderScanTimeAsync(connection, transaction, folderId, cancellationToken);

        transaction.Commit();
        return folderId;
    }

    private async Task<long> UpsertFolderAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IndexedFolder folder,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO IndexedFolders (ParentFolderId, Name, FullPath)
            VALUES (@parentFolderId, @name, @fullPath)
            ON CONFLICT (FullPath) DO UPDATE SET
                ParentFolderId = excluded.ParentFolderId,
                Name = excluded.Name,
                FullPath = excluded.FullPath
            RETURNING Id;
            """;
        command.Parameters.AddWithValue("@parentFolderId", folder.ParentFolderId is null
            ? DBNull.Value
            : folder.ParentFolderId.Value);
        command.Parameters.AddWithValue("@name", folder.Name);
        command.Parameters.AddWithValue("@fullPath", folder.FullPath);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is not long folderId)
            throw new InvalidOperationException($"Could not save indexed folder '{folder.FullPath}'.");

        return folderId;
    }

    private async Task UpsertFilesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long folderId,
        IReadOnlyList<IndexedFile> files,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO IndexedFiles (FolderId, Name, Extension, SizeBytes, LastModifiedUtcTicks)
            VALUES (@folderId, @name, @extension, @sizeBytes, @lastModifiedUtcTicks)
            ON CONFLICT (FolderId, Name) DO UPDATE SET
                Extension = excluded.Extension,
                SizeBytes = excluded.SizeBytes,
                LastModifiedUtcTicks = excluded.LastModifiedUtcTicks
            RETURNING Id;
            """;

        var folderIdParameter = command.Parameters.Add("@folderId", SqliteType.Integer);
        var nameParameter = command.Parameters.Add("@name", SqliteType.Text);
        var extensionParameter = command.Parameters.Add("@extension", SqliteType.Text);
        var sizeParameter = command.Parameters.Add("@sizeBytes", SqliteType.Integer);
        var modifiedParameter = command.Parameters.Add("@lastModifiedUtcTicks", SqliteType.Integer);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            folderIdParameter.Value = folderId;
            nameParameter.Value = file.Name;
            extensionParameter.Value = file.Extension;
            sizeParameter.Value = file.SizeBytes;
            modifiedParameter.Value = file.LastModifiedUtcTicks;

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is not long fileId)
                throw new InvalidOperationException($"Could not save indexed file '{file.Name}'.");

            file.Id = fileId;
            file.FolderId = folderId;
        }
    }

    private async Task UpdateFolderScanTimeAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long folderId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE IndexedFolders SET LastScannedUtcTicks = @lastScannedUtcTicks WHERE Id = @id;";
        command.Parameters.AddWithValue("@lastScannedUtcTicks", DateTime.UtcNow.Ticks);
        command.Parameters.AddWithValue("@id", folderId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private bool TryCreateIndexedFile(
        string filePath,
        List<IndexingError> errors,
        out IndexedFile? indexedFile,
        out bool isReparsePoint)
    {
        indexedFile = null;
        isReparsePoint = false;

        if (!TryGetAttributes(filePath, errors, out var attributes))
            return false;

        isReparsePoint = (attributes & FileAttributes.ReparsePoint) != 0;
        if (isReparsePoint)
            return true;

        try
        {
            var fileInfo = new FileInfo(filePath);
            fileInfo.Refresh();
            indexedFile = new IndexedFile
            {
                Name = fileInfo.Name,
                Extension = fileInfo.Extension,
                SizeBytes = fileInfo.Length,
                LastModifiedUtcTicks = fileInfo.LastWriteTimeUtc.Ticks
            };
            return true;
        }
        catch (IOException exception)
        {
            errors.Add(new IndexingError(filePath, exception.Message));
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            errors.Add(new IndexingError(filePath, exception.Message));
            return false;
        }
    }

    private bool TryGetAttributes(
        string path,
        List<IndexingError> errors,
        out FileAttributes attributes)
    {
        try
        {
            attributes = File.GetAttributes(path);
            return true;
        }
        catch (IOException exception)
        {
            errors.Add(new IndexingError(path, exception.Message));
        }
        catch (UnauthorizedAccessException exception)
        {
            errors.Add(new IndexingError(path, exception.Message));
        }

        attributes = default;
        return false;
    }

    private void ConfigureExcludedDirectories()
    {
        if (Path.DirectorySeparatorChar == '\\')
        {
            AddExcludedDirectory(Environment.SpecialFolder.Windows);
            AddExcludedDirectory(Environment.SpecialFolder.ProgramFiles);
            AddExcludedDirectory(Environment.SpecialFolder.ProgramFilesX86);
            AddExcludedDirectory(Environment.SpecialFolder.CommonApplicationData);
            return;
        }

        _excludedDirectoryPaths.AddRange(
        [
            "/proc",
            "/sys",
            "/dev",
            "/run",
            "/tmp",
            "/etc",
            "/usr",
            "/bin",
            "/sbin",
            "/lib",
            "/lib64",
            "/boot",
            "/root",
            "/var",
            "/lost+found"
        ]);
    }

    private void AddExcludedDirectory(Environment.SpecialFolder specialFolder)
    {
        var path = Environment.GetFolderPath(specialFolder);
        if (!string.IsNullOrWhiteSpace(path))
            _excludedDirectoryPaths.Add(Path.GetFullPath(path));
    }

    private bool IsExcludedDirectory(string path)
    {
        var fullPath = Path.GetFullPath(path);
        foreach (var excludedPath in _excludedDirectoryPaths)
        {
            if (IsSameOrDescendant(fullPath, excludedPath))
                return true;
        }

        if (Path.DirectorySeparatorChar == '\\')
        {
            var rootPath = Path.GetPathRoot(fullPath);
            var parentPath = Directory.GetParent(fullPath)?.FullName;
            if (rootPath is not null && parentPath is not null && _pathComparer.Equals(rootPath, parentPath))
            {
                var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(fullPath));
                if (name.Equals("$Recycle.Bin", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Recovery", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsSameOrDescendant(string path, string directoryPath)
    {
        var normalizedPath = Path.TrimEndingDirectorySeparator(path);
        var normalizedDirectory = Path.TrimEndingDirectorySeparator(directoryPath);
        return _pathComparer.Equals(normalizedPath, normalizedDirectory) ||
               normalizedPath.StartsWith(
                   normalizedDirectory + Path.DirectorySeparatorChar,
                   Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private sealed record FolderScanResult(
        long FolderId,
        IReadOnlyList<string> ChildDirectories,
        IReadOnlyList<IndexedFile> IndexedFiles);
}
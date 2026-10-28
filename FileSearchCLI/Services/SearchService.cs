using System.Runtime.CompilerServices;
using FileSearchCLI.Models;
using Microsoft.Data.Sqlite;

namespace FileSearchCLI.Services;

public sealed class SearchService
{
    private readonly SqliteDatabaseService _databaseService;

    public SearchService(SqliteDatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async IAsyncEnumerable<FileSearchResult> SearchAsync(
        string? fileNamePattern,
        IReadOnlyList<string> fileFormats,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileFormats);

        await _databaseService.InitializeAsync(cancellationToken);
        await using var connection = await _databaseService.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(fileNamePattern))
        {
            conditions.Add("f.Name LIKE @namePattern ESCAPE '\\'");
            command.Parameters.AddWithValue("@namePattern", $"%{EscapeLikePattern(fileNamePattern)}%");
        }

        if (fileFormats.Count > 0)
        {
            var formatParameters = new List<string>(fileFormats.Count);
            for (var index = 0; index < fileFormats.Count; index++)
            {
                var parameterName = $"@format{index}";
                formatParameters.Add(parameterName);
                command.Parameters.AddWithValue(parameterName, fileFormats[index]);
            }

            conditions.Add($"f.Extension IN ({string.Join(", ", formatParameters)})");
        }

        command.CommandText = $"""
            SELECT folder.FullPath, f.Name, f.Extension, f.SizeBytes
            FROM IndexedFiles AS f
            INNER JOIN IndexedFolders AS folder ON folder.Id = f.FolderId
            {(conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : string.Empty)}
            ORDER BY f.Name COLLATE NOCASE, folder.FullPath COLLATE NOCASE;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var folderPath = reader.GetString(0);
            var name = reader.GetString(1);
            yield return new FileSearchResult(
                Path.Combine(folderPath, name),
                name,
                reader.GetString(2),
                reader.GetInt64(3));
        }
    }

    private string EscapeLikePattern(string pattern)
    {
        return pattern
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
using FileSearchCLI.Interface;
using FileSearchCLI.Services;

namespace FileSearchCLI.Operations;

public sealed class RemoveDatabaseOperation(SqliteDatabaseService databaseService) : IOperation
{
    public string ShortKey => "-r";

    public string LongKey => "--remove-database";

    public string Description => "Remove the existing search database.";

    public bool SupportsStreaming => false;

    public Dictionary<string, string> Arguments { get; } = new();

    public Task<OperationResult> ExecuteAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var result = databaseService.RemoveDatabase()
            ? "Database removed."
            : "No database exists.";
        return Task.FromResult(new OperationResult("Database", result));
    }

    public IAsyncEnumerable<OperationResult> StreamAsync(CancellationToken ct = default)
    {
        throw new NotSupportedException("Database removal does not support streaming.");
    }

    public void SetArguments(Dictionary<string, string> arguments)
    {
        if (arguments.Count > 0)
            throw new ArgumentException("Database removal does not accept arguments.", nameof(arguments));
    }
}
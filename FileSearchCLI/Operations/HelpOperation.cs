using FileSearchCLI.Interface;
using FileSearchCLI.Services.OperationServices;

namespace FileSearchCLI.Operations;

public sealed class HelpOperation(OperationService operationService) : IOperation
{
    public string ShortKey => "-h";

    public string LongKey => "--help";

    public string Description => "Display all available operations.";

    public bool SupportsStreaming => false;

    public Dictionary<string, string> Arguments { get; } = new();

    public Task<OperationResult> ExecuteAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var lines = new List<string> { "Available operations:" };
        foreach (var operation in operationService.GetAvailableOperations())
        {
            lines.Add($"  {operation.ShortKey}, {operation.LongKey} - {operation.Description}");

            var argumentAliases = operation.Arguments
                .GroupBy(argument => argument.Value)
                .Select(group => string.Join(", ", group.Select(argument => argument.Key)));

            foreach (var aliases in argumentAliases)
                lines.Add($"    Options: {aliases}");
        }

        return Task.FromResult(new OperationResult("Help", string.Join(Environment.NewLine, lines)));
    }

    public IAsyncEnumerable<OperationResult> StreamAsync(CancellationToken ct = default)
    {
        throw new NotSupportedException("The help operation does not support streaming.");
    }

    public void SetArguments(Dictionary<string, string> arguments)
    {
        if (arguments.Count > 0)
            throw new ArgumentException("The help operation does not accept arguments.", nameof(arguments));
    }
}
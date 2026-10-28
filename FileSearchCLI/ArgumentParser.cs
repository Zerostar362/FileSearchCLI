using FileSearchCLI.Interface;
using FileSearchCLI.Services.OperationServices;

public sealed class ArgumentParser
{
    private OperationService _operationService;

    public ArgumentParser(OperationService operationService)
    {
        _operationService = operationService;
    }

    public IOperation Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            return GetOperation("--help");

        var command = args[0];

        var operation = GetOperation(command);

        var arguments = ParseArguments(args.Skip(1).ToList());

        operation.SetArguments(arguments);
        return operation;
    }

    private static Dictionary<string, string> ParseArguments(IReadOnlyList<string> args)
    {
        var arguments = new Dictionary<string, string>();

        for (var i = 0; i < args.Count; i++)
        {
            var token = args[i];

            if (!token.StartsWith('-'))
            {
                throw new ArgumentException($"Expected a flag starting with '-' but found '{token}'.");
            }

            var hasValue = i + 1 < args.Count && !args[i + 1].StartsWith('-');
            var value = hasValue ? args[++i] : string.Empty;

            arguments[token] = value;
        }

        return arguments;
    }

    private IOperation GetOperation(string command) 
    {
        var (option, inlineValue) = SplitArgument(command);
        if (inlineValue is not null)
            throw new ArgumentException($"Command '{option}' does not accept a value.", nameof(command));

        if (!_operationService.TryGetOperation(option, out var operation) || operation is null)
            throw new ArgumentException($"Unknown command '{command}'.", nameof(command));

        return operation;
    }

    private (string Option, string? InlineValue) SplitArgument(string argument)
    {
        var separatorIndex = argument.IndexOf('=');
        return separatorIndex >= 0
            ? (argument[..separatorIndex], argument[(separatorIndex + 1)..])
            : (argument, null);
    }

}

using System.Collections.ObjectModel;
using FileSearchCLI.Interface;
using FileSearchCLI.Models;
using FileSearchCLI.Services.OperationServices;

public sealed class SearchOptions
{
    public bool Index { get; }
    public bool Search { get; }
    public bool RemoveDatabase { get; }
    public string? FileNamePattern { get; }
    public IReadOnlyList<string> FileFormats { get; }

    public SearchOptions(
        bool index,
        bool search,
        bool removeDatabase,
        string? fileNamePattern,
        IReadOnlyList<string> fileFormats)
    {
        Index = index;
        Search = search;
        RemoveDatabase = removeDatabase;
        FileNamePattern = fileNamePattern;
        FileFormats = fileFormats;
    }

    public bool Matches(IndexedFile file)
    {
        if (FileNamePattern is not null &&
            !file.Name.Contains(FileNamePattern, StringComparison.OrdinalIgnoreCase))
            return false;

        if (FileFormats.Count == 0)
            return true;

        foreach (var format in FileFormats)
        {
            if (string.Equals(file.Extension, format, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

[Obsolete]
public sealed class ArgumentParseResult
{
    public SearchOptions? Options { get; }
    public string? ErrorMessage { get; }
    public bool ShowHelp { get; }
    public bool IsSuccessful => Options is not null && ErrorMessage is null;

    public ArgumentParseResult(SearchOptions? options, string? errorMessage, bool showHelp)
    {
        Options = options;
        ErrorMessage = errorMessage;
        ShowHelp = showHelp;
    }
}

public sealed class ArgumentParser
{
    private OperationService _operationService;

    public ArgumentParser(OperationService operationService)
    {
        _operationService = operationService;
    }

    public IOperation Parse(IReadOnlyList<string> args)
    {
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

    /*public ArgumentParseResult Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return CreateHelpResult();
        }

        var indexRequested = false;
        var searchRequested = false;
        var removeDatabaseRequested = false;
        string? fileNamePattern = null;
        var fileFormats = new List<string>();

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];

            if (IsHelpArgument(argument))
                return CreateHelpResult();

            var error = ProcessArgument(
                args,
                ref index,
                argument,
                ref indexRequested,
                ref searchRequested,
                ref removeDatabaseRequested,
                ref fileNamePattern,
                fileFormats);
            if (error is not null)
            {
                return error;
            }
        }

        if (removeDatabaseRequested &&
            (indexRequested || searchRequested || fileNamePattern is not null || fileFormats.Count > 0))
            return new ArgumentParseResult(null, "Database removal cannot be combined with other commands or filters.", false);

        return CreateSuccessResult(
            indexRequested,
            searchRequested,
            removeDatabaseRequested,
            fileNamePattern,
            fileFormats);
    }*/

    private IOperation GetOperation(string command) 
    {
        var (option, inlineValue) = SplitArgument(command);
        _operationService.TryGetOperation(option, out var operation);
        return operation;
    }

    private ArgumentParseResult? ProcessArgument(
        IReadOnlyList<string> args,
        ref int index,
        string argument,
        ref bool indexRequested,
        ref bool searchRequested,
        ref bool removeDatabaseRequested,
        ref string? fileNamePattern,
        List<string> fileFormats)
    {
        var (option, inlineValue) = SplitArgument(argument);
        if (option is "-i" or "--index")
        {
            if (inlineValue is not null)
                throw new ArgumentOutOfRangeException($"Option '{option}' does not accept a value.");
            //return new ArgumentParseResult(null, $"Option '{option}' does not accept a value.", false);

            if (indexRequested)
                throw new ArgumentException("The index option can only be specified once.");
                //return new ArgumentParseResult(null, "The index option can only be specified once.", false);

            if (searchRequested)
                throw new ArgumentException("Indexing and searching cannot be requested together.");
                //return new ArgumentParseResult(null, "Indexing and searching cannot be requested together.", false);

            indexRequested = true;
            return null;
        }

        if (option is "-r" or "--remove-database")
        {
            if (inlineValue is not null)
                throw new ArgumentOutOfRangeException($"Option '{option}' does not accept a value.");
                //return new ArgumentParseResult(null, $"Option '{option}' does not accept a value.", false);

            if (removeDatabaseRequested)
                throw new ArgumentException("The remove-database option can only be specified once.");
            //return new ArgumentParseResult(null, "The remove-database option can only be specified once.", false);

            removeDatabaseRequested = true;
            return null;
        }

        if (option is "-s" or "--search")
        {
            if (inlineValue is not null)
                throw new ArgumentOutOfRangeException($"Option '{option}' does not accept a value.");
            //return new ArgumentParseResult(null, $"Option '{option}' does not accept a value.", false);

            if (searchRequested)
                throw new ArgumentException("The search option can only be specified once.");
                //return new ArgumentParseResult(null, "The search option can only be specified once.", false);

            if (indexRequested)
                throw new ArgumentException("Indexing and searching cannot be requested together.");
                //return new ArgumentParseResult(null, "Indexing and searching cannot be requested together.", false);

            searchRequested = true;
            return null;
        }

        if (option is "-n" or "--name")
            return ProcessNameOption(args, ref index, option, inlineValue, ref fileNamePattern);

        if (option is "-f" or "--format")
            return ProcessFormatOption(args, ref index, option, inlineValue, fileFormats);

        return new ArgumentParseResult(null, $"Unknown argument '{argument}'.", false);
    }

    private ArgumentParseResult? ProcessNameOption(
        IReadOnlyList<string> args,
        ref int index,
        string option,
        string? inlineValue,
        ref string? fileNamePattern)
    {
        if (fileNamePattern is not null)
        {
            return new ArgumentParseResult(null, "The name filter can only be specified once.", false);
        }

        if (!TryReadValue(args, ref index, inlineValue, option, out var value, out var errorMessage))
        {
            return new ArgumentParseResult(null, errorMessage, false);
        }

        fileNamePattern = value;
        return null;
    }

    private ArgumentParseResult? ProcessFormatOption(
        IReadOnlyList<string> args,
        ref int index,
        string option,
        string? inlineValue,
        List<string> fileFormats)
    {
        if (!TryReadValue(args, ref index, inlineValue, option, out var value, out var errorMessage))
        {
            return new ArgumentParseResult(null, errorMessage, false);
        }

        if (!TryNormalizeFormat(value, out var normalizedFormat))
        {
            return new ArgumentParseResult(null, $"'{value}' is not a valid file format. Use an extension such as '.txt' or 'txt'.", false);
        }

        AddFormatIfMissing(fileFormats, normalizedFormat);
        return null;
    }

    private (string Option, string? InlineValue) SplitArgument(string argument)
    {
        var separatorIndex = argument.IndexOf('=');
        return separatorIndex >= 0
            ? (argument[..separatorIndex], argument[(separatorIndex + 1)..])
            : (argument, null);
    }

    private bool IsHelpArgument(string argument)
    {
        return argument is "-h" or "--help";
    }

    private void AddFormatIfMissing(List<string> fileFormats, string format)
    {
        foreach (var existingFormat in fileFormats)
        {
            if (string.Equals(existingFormat, format, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        fileFormats.Add(format);
    }

    private ArgumentParseResult CreateHelpResult()
    {
        return new ArgumentParseResult(null, null, true);
    }

    private ArgumentParseResult CreateSuccessResult(
        bool index,
        bool search,
        bool removeDatabase,
        string? fileNamePattern,
        List<string> fileFormats)
    {
        return new ArgumentParseResult(
            new SearchOptions(index, search, removeDatabase, fileNamePattern, new ReadOnlyCollection<string>(fileFormats)),
            null,
            false);
    }

    public string GetUsage()
    {
        return "Usage: FileSearchCLI [options]\n" +
               "Commands:\n" +
               "  -i, --index            Index files and folders on available drives\n" +
               "  -s, --search           Search files already in the index\n" +
               "  -r, --remove-database  Remove the existing search database\n" +
               "\n" +
               "Search filters (use with -s; both are optional):\n" +
               "  -n, --name <text>      Match file names containing <text> (case-insensitive)\n" +
               "  -f, --format <ext>     Match a file extension, e.g. txt, .txt, or *.txt; repeatable\n" +
               "  Name and format filters combine with AND; multiple formats combine with OR.\n" +
               "  Search prints matching file paths. Without filters, all indexed files are returned.\n" +
               "  -h, --help             Show this help\n" +
               "\n" +
               "Examples:\n" +
               "  FileSearchCLI -i\n" +
               "  FileSearchCLI -r\n" +
               "  FileSearchCLI -s\n" +
               "  FileSearchCLI -s -n report\n" +
               "  FileSearchCLI -s -f txt -f pdf\n" +
               "  FileSearchCLI -s -n report -f pdf";
    }

    private bool TryReadValue(
        IReadOnlyList<string> args,
        ref int index,
        string? inlineValue,
        string option,
        out string value,
        out string errorMessage)
    {
        if (inlineValue is null)
        {
            if (index + 1 >= args.Count || args[index + 1].StartsWith("-", StringComparison.Ordinal))
            {
                value = string.Empty;
                errorMessage = $"Option '{option}' requires a value.";
                return false;
            }

            index++;
            inlineValue = args[index];
        }

        if (string.IsNullOrWhiteSpace(inlineValue))
        {
            value = string.Empty;
            errorMessage = $"Option '{option}' requires a non-empty value.";
            return false;
        }

        value = inlineValue.Trim();
        errorMessage = string.Empty;
        return true;
    }

    private bool TryNormalizeFormat(string format, out string normalizedFormat)
    {
        normalizedFormat = format.StartsWith("*.", StringComparison.Ordinal)
            ? format[1..]
            : format.StartsWith(".", StringComparison.Ordinal)
                ? format
                : $".{format}";

        if (normalizedFormat.Length <= 1)
        {
            return false;
        }

        foreach (var character in normalizedFormat)
        {
            if (char.IsWhiteSpace(character) || character is '/' or '\\' or '*' or '?')
            {
                return false;
            }
        }

        return true;
    }
}

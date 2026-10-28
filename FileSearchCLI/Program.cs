using FileSearchCLI.Services;
using FileSearchCLI.Services.OperationServices;

void WriteResult(string name, string result)
{
    var output = $"[{name}]: {result}";
    if (Console.IsOutputRedirected)
    {
        Console.WriteLine(output);
        return;
    }

    var maxLineLength = Console.WindowWidth - 1;
    if (maxLineLength <= 0)
    {
        Console.WriteLine(output);
        return;
    }

    foreach (var line in output.Split(Environment.NewLine))
    {
        if (line.Length <= maxLineLength)
        {
            Console.WriteLine(line);
            continue;
        }

        Console.WriteLine(maxLineLength <= 3
            ? line[..maxLineLength]
            : $"{line[..(maxLineLength - 3)]}...");
    }
}

var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var databasePath = Path.Combine(applicationDataPath, "FileSearchCLI", "FileSearch.db");
var databaseService = new SqliteDatabaseService(databasePath);
var operationsService = new OperationService(databaseService);
var parser = new ArgumentParser(operationsService);
var result = parser.Parse(args);

if (result == null)
{
    Console.Error.WriteLine("Failed to parse arguments.");
    Environment.ExitCode = 1;
    return;
}

if (result.SupportsStreaming)
{
    await foreach (var operationResult in result.StreamAsync())
    {
        WriteResult(operationResult.Name, operationResult.Result);
    }
    return;
}
else
{
    var operationResult = await result.ExecuteAsync();
    WriteResult(operationResult.Name, operationResult.Result);
    return;
}

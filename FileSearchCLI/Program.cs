using FileSearchCLI.Services;
using FileSearchCLI.Services.OperationServices;

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
        Console.WriteLine($"[{operationResult.Name}]: {operationResult.Result}");
    }
    return;
}
else
{
    var operationResult = await result.ExecuteAsync();
    Console.WriteLine($"[{operationResult.Name}]: {operationResult.Result}");
    return;
}


/*if (result.ShowHelp)
{
    Console.WriteLine(parser.GetUsage());
    return;
}

if (!result.IsSuccessful)
{
    Console.Error.WriteLine($"Error: {result.ErrorMessage}");
    Console.Error.WriteLine(parser.GetUsage());
    Environment.ExitCode = 2;
    return;
}

if (result.Options!.RemoveDatabase)
{
    var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    if (string.IsNullOrWhiteSpace(applicationDataPath))
    {
        Console.Error.WriteLine("Could not determine the local application data directory for the database.");
        Environment.ExitCode = 1;
        return;
    }

    var databasePath = Path.Combine(applicationDataPath, "FileSearchCLI", "FileSearch.db");
    var databaseService = new SqliteDatabaseService(databasePath);
    Console.WriteLine(databaseService.RemoveDatabase()
        ? "Database removed."
        : "No database exists.");
    return;
}

if (result.Options!.Index)
{
    var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    if (string.IsNullOrWhiteSpace(applicationDataPath))
    {
        Console.Error.WriteLine("Could not determine the local application data directory for the database.");
        Environment.ExitCode = 1;
        return;
    }

    var databasePath = Path.Combine(applicationDataPath, "FileSearchCLI", "FileSearch.db");
    var databaseService = new SqliteDatabaseService(databasePath);
    var indexerService = new IndexerService(databaseService);
    var itemCount = 0;

    await foreach (var indexingEvent in indexerService.IndexAllDrivesAsync())
    {
        if (indexingEvent.IndexedFile is not null && result.Options.Matches(indexingEvent.IndexedFile))
            Console.WriteLine($"[{++itemCount}]:Indexed:{indexingEvent.IndexedFile.Name}");
        else if (indexingEvent.Error is not null)
            Console.WriteLine($"Error: {indexingEvent.Error.Path}: {indexingEvent.Error.Message}");
    }

    return;
}

if (result.Options.Search)
{
    var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    if (string.IsNullOrWhiteSpace(applicationDataPath))
    {
        Console.Error.WriteLine("Could not determine the local application data directory for the database.");
        Environment.ExitCode = 1;
        return;
    }

    var databasePath = Path.Combine(applicationDataPath, "FileSearchCLI", "FileSearch.db");
    var databaseService = new SqliteDatabaseService(databasePath);
    var searchService = new SearchService(databaseService);

    await foreach (var searchResult in searchService.SearchAsync(
        result.Options.FileNamePattern,
        result.Options.FileFormats))
        Console.WriteLine(searchResult.FullPath);

    return;
}

Console.WriteLine("Search criteria:");
if (result.Options!.FileNamePattern is not null)
{
    Console.WriteLine($"  Name: {result.Options.FileNamePattern}");
}

if (result.Options.FileFormats.Count == 0)
{
    Console.WriteLine("  Format: any");
}
else
{
    Console.WriteLine("  Formats:");
    foreach (var format in result.Options.FileFormats)
    {
        Console.WriteLine($"    {format}");
    }
}*/

namespace FileSearchCLI.Models;

public sealed record IndexingError(string Path, string Message);
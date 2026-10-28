namespace FileSearchCLI.Models;

public sealed record FileSearchResult(string FullPath, string Name, string Extension, long SizeBytes);
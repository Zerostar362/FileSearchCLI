namespace FileSearchCLI.Models;

public sealed class IndexedFile
{
    public long Id { get; set; }
    public long FolderId { get; set; }
    public required string Name { get; set; }
    public required string Extension { get; set; }
    public long SizeBytes { get; set; }
    public long LastModifiedUtcTicks { get; set; }
}

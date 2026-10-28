namespace FileSearchCLI.Models;

public sealed class IndexedFolder
{
    public long Id { get; set; }
    public long? ParentFolderId { get; set; }
    public required string Name { get; set; }
    public required string FullPath { get; set; }
    public long? LastScannedUtcTicks { get; set; }
}

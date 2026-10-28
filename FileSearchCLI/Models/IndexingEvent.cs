namespace FileSearchCLI.Models;

public sealed record IndexingEvent
{
    public IndexedFile? IndexedFile { get; }
    public IndexingError? Error { get; }

    public IndexingEvent(IndexedFile indexedFile)
    {
        IndexedFile = indexedFile;
    }

    public IndexingEvent(IndexingError error)
    {
        Error = error;
    }
}
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

    public override string ToString()
    {
        if (IndexedFile is not null)
        {
            return $"{IndexedFile.Name}";
        }
        else if (Error is not null)
        {
            return $"{Error.Message}";
        }
        else
        {
            return "Unknown event";
        }
    }
}
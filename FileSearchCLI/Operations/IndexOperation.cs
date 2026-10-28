using FileSearchCLI.Interface;
using FileSearchCLI.Services;
using System.Runtime.CompilerServices;

namespace FileSearchCLI.Operations
{
    public class IndexOperation(IndexerService _indexerService) : IOperation
    {
        public string ShortKey => "-i";

        public string LongKey => "--index";

        public string Description => "Index files and folders on available drives.";

        public bool SupportsStreaming => true;

        public Dictionary<string, string> Arguments { get; } = new();

        public Task<OperationResult> ExecuteAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public async IAsyncEnumerable<OperationResult> StreamAsync([EnumeratorCancellation]CancellationToken ct = default)
        {
            await foreach (var result in _indexerService.IndexAllDrivesAsync(ct))
                yield return new OperationResult("Indexing", result.ToString());
        }

        public void SetArguments(Dictionary<string, string> arguments)
        {
            // No arguments to set for this operation
        }

    }
}

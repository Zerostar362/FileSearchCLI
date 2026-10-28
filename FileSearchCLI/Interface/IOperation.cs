using System.Collections.Generic;

namespace FileSearchCLI.Interface
{
    public interface IOperation
    {
        public string ShortKey { get; }
        public string LongKey { get; }
        public string Description { get; }
        public bool SupportsStreaming { get; }
        public Dictionary<string, string> Arguments { get; }

        public IAsyncEnumerable<OperationResult> StreamAsync(CancellationToken ct = default);
        public Task<OperationResult> ExecuteAsync(CancellationToken ct = default);
        public void SetArguments(Dictionary<string, string> arguments);
    }

    public record OperationResult(string Name, string Result);
}

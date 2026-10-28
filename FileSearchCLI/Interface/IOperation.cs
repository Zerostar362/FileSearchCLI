using System;
using System.Collections.Generic;
using System.Text;

namespace FileSearchCLI.Interface
{
    public interface IOperation
    {
        public string ShortKey { get; }
        public string LongKey { get; }
        public bool SupportsStreaming { get; }
        public Dictionary<string, string> Arguments { get; }
        public OperationDescription GetDescription();
        public void Validate();

        public IAsyncEnumerable<OperationResult> StreamAsync(CancellationToken ct = default);
        public Task<OperationResult> ExecuteAsync(CancellationToken ct = default);
        public void SetArguments(Dictionary<string, string> arguments);
    }

    public record OperationDescription(string ShortKey, string LongKey, string Description, Dictionary<string, string> arguments);
    public record OperationResult(string Name, string Result);
}

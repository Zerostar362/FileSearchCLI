using FileSearchCLI.Interface;
using FileSearchCLI.Models;
using FileSearchCLI.Services;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FileSearchCLI.Operations
{
    public class IndexOperation(IndexerService _indexerService) : IOperation
    {
        public string ShortKey => "i";

        public string LongKey => "index";

        public bool SupportsStreaming => true;

        public Dictionary<string, string> Arguments { get; } = new();

        public Task<OperationResult> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public OperationDescription GetDescription()
        {
            return new OperationDescription(ShortKey, LongKey, "Indexes files in the specified directory.", new Dictionary<string, string>
            {
                {"No arguments","" }
            });
        }

        public async IAsyncEnumerable<OperationResult> StreamAsync(Dictionary<string, object> arguments, [EnumeratorCancellation]CancellationToken ct = default)
        {
            await foreach (var result in _indexerService.IndexAllDrivesAsync(ct))
                yield return new OperationResult("Indexing", result.ToString());
        }

        public void Validate()
        {
            
        }
    }
}

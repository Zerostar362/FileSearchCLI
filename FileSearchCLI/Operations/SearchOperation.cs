using FileSearchCLI.Interface;
using FileSearchCLI.Models;
using FileSearchCLI.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileSearchCLI.Operations
{
    public class SearchOperation(SearchService _service) : IOperation
    {
        public string ShortKey => "s";

        public string LongKey => "search";

        public bool SupportsStreaming => true;

        public Dictionary<string, string> Arguments
        {
            get => new Dictionary<string, string>
            {
                {"-n","name" }, {"--name","name" }, {"-f", "format"}, {"--format", "format" }
            };
        }

        public Task<OperationResult> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public OperationDescription GetDescription()
        {
            return new OperationDescription(ShortKey, LongKey, "Searches for files in the indexed database.", new Dictionary<string, string>
            {
                {"-n","name" }, {"--name","name" }, {"-f", "format"}, {"--format", "format" }
            });
        }

        public async IAsyncEnumerable<OperationResult> StreamAsync(Dictionary<string, object> arguments,CancellationToken ct = default)
        {
            var name = arguments.ContainsKey("name") ? arguments["name"]?.ToString() : null;
            var format = arguments.ContainsKey("format") ? arguments["format"]?.ToString() : null;
            await foreach (var result in _service.SearchAsync(name, [format], ct))
                yield return new OperationResult("Search", result.ToString());
        }

        public void Validate()
        {
            throw new NotImplementedException();
        }
    }
}

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
        public string ShortKey => "-s";

        public string LongKey => "--search";

        public bool SupportsStreaming => true;

        public Dictionary<string, string> Arguments
        {
            get => new Dictionary<string, string>
            {
                {"-n","name" }, {"--name","name" }, {"-f", "format"}, {"--format", "format" }
            };
        }

        private Dictionary<string, string> _realArguments = new();

        public Task<OperationResult> ExecuteAsync(CancellationToken ct = default)
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

        public async IAsyncEnumerable<OperationResult> StreamAsync(CancellationToken ct = default)
        {
            var name = _realArguments.ContainsKey("name") ? _realArguments["name"]?.ToString() : null;
            var format = _realArguments.ContainsKey("format") ? _realArguments["format"]?.ToString() : null;
            var array = format is not null ? new[] { format } : Array.Empty<string>();
            await foreach (var result in _service.SearchAsync(name, array, ct))
                yield return new OperationResult("Search", result.ToString());
        }

        public void SetArguments(Dictionary<string, string> arguments)
        {
            foreach (var arg in arguments)
            {
                if (!Arguments.ContainsKey(arg.Key))
                {
                    throw new ArgumentException($"Unknown argument '{arg.Key}' for operation '{LongKey}'.");
                }

                var value = Arguments[arg.Key];
                _realArguments.Add(value, arg.Value);
            }
        }

        public void Validate()
        {
            throw new NotImplementedException();
        }
    }
}

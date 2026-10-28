using FileSearchCLI.Interface;
using FileSearchCLI.Services;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FileSearchCLI.Operations
{
    public class RemoveDatabaseOperation : IOperation
    {
        public string ShortKey => "r";

        public string LongKey => "remove";

        public bool SupportsStreaming => false;
        public Dictionary<string, string> Arguments { get; } = new();

        public Task<OperationResult> ExecuteAsync(Dictionary<string, object> arguments, CancellationToken ct = default)
        {
            var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(applicationDataPath))
            {
                Console.Error.WriteLine("Could not determine the local application data directory for the database.");
                Environment.ExitCode = 1;
                return Task.FromResult(new OperationResult("RemoveDatabase", "Failed to determine the local application data directory."));
            }

            var databasePath = Path.Combine(applicationDataPath, "FileSearchCLI", "FileSearch.db");
            var databaseService = new SqliteDatabaseService(databasePath);
            Console.WriteLine(databaseService.RemoveDatabase()
                ? "Database removed."
                : "No database exists.");
            return Task.FromResult(new OperationResult("RemoveDatabase", "Database removed."));
        }

        public OperationDescription GetDescription()
        {
            return new OperationDescription(ShortKey, LongKey, "Removes the database file.", new Dictionary<string, string>
            {
                {"No arguments","" }
            });
        }

        public IAsyncEnumerable<OperationResult> StreamAsync(Dictionary<string, object> arguments, [EnumeratorCancellation]CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public void Validate()
        {
            
        }
    }
}

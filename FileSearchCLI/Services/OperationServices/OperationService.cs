using FileSearchCLI.Interface;
using FileSearchCLI.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileSearchCLI.Services.OperationServices
{
    public class OperationService
    {
        private Dictionary<string, IOperation> _operations;
        private SqliteDatabaseService _service;

        public OperationService(SqliteDatabaseService service)
        {
            _service = service;
            _operations = new Dictionary<string, IOperation>();
            LoadOperations();
        }

        public void AddOperation(string key, IOperation operation)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
            }
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation), "Operation cannot be null.");
            }
            _operations[key] = operation;
        }

        public bool TryGetOperation(string key, out IOperation? operation)
        {
            return _operations.TryGetValue(key, out operation);
        }

        private Task LoadOperations()
        {
            // Load operations from configuration or other sources
            // For example:
            AddOperation("index", new IndexOperation(new IndexerService(_service)));
            AddOperation("search", new SearchOperation(new SearchService(_service)));
            AddOperation("remove-database", new RemoveDatabaseOperation());
            return Task.CompletedTask;
        }
    }
}

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

        public void AddOperation(IOperation operation)
        {
            foreach (var key in new[] { operation.ShortKey, operation.LongKey })
            {
                if (!string.IsNullOrWhiteSpace(key))
                {       
                    _operations[key] = operation;
                }
            }
        }

        public bool TryGetOperation(string key, out IOperation? operation)
        {
            return _operations.TryGetValue(key, out operation);
        }

        private Task LoadOperations()
        {
            // Load operations from configuration or other sources
            // For example:
            AddOperation(new IndexOperation(new IndexerService(_service)));
            AddOperation(new SearchOperation(new SearchService(_service)));
            //AddOperation("remove-database", new RemoveDatabaseOperation());
            return Task.CompletedTask;
        }
    }
}

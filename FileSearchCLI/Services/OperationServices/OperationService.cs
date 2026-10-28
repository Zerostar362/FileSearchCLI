using FileSearchCLI.Interface;
using FileSearchCLI.Operations;
using System.Collections.Generic;

namespace FileSearchCLI.Services.OperationServices
{
    public class OperationService
    {
        private readonly Dictionary<string, IOperation> _operations;
        private readonly SqliteDatabaseService _service;

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

        public IReadOnlyCollection<IOperation> GetAvailableOperations()
        {
            return _operations.Values.Distinct().ToArray();
        }

        private void LoadOperations()
        {
            AddOperation(new IndexOperation(new IndexerService(_service)));
            AddOperation(new SearchOperation(new SearchService(_service)));
            AddOperation(new RemoveDatabaseOperation(_service));
            AddOperation(new HelpOperation(this));
        }
    }
}

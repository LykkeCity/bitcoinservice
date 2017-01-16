using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using Core.Exceptions;
using Core.Repositories.TransactionOutputs;
using Microsoft.WindowsAzure.Storage;

namespace AzureRepositories.TransactionOutputs
{
    public class OutputEntity : TableEntity, IOutput
    {
        public static string GeneratePartitionKey()
        {
            return "SpentOutput";
        }

        public static string GenerateRowKey(string hash, int n)
        {
            return $"{hash}_{n}";
        }

        public string TransactionHash { get; set; }
        public int N { get; set; }

        public static OutputEntity Create(IOutput output)
        {
            return new OutputEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(output.TransactionHash, output.N),
                TransactionHash = output.TransactionHash,
                N = output.N
            };
        }
    }

    public class SpentOutputRepository : ISpentOutputRepository
    {
        private const int EntityExistsHttpStatusCode = 409;

        private readonly INoSQLTableStorage<OutputEntity> _storage;

        public SpentOutputRepository(INoSQLTableStorage<OutputEntity> storage)
        {
            _storage = storage;
        }

        public Task InsertSpentOutputs(IEnumerable<IOutput> outputs)
        {
            Action<StorageException> throwIfBackend = (exception) =>
            {
                if (exception != null && exception.RequestInformation.HttpStatusCode == EntityExistsHttpStatusCode)
                    throw new BackendException("entity already exists", ErrorCode.TransactionConcurrentInputsProblem);
            };

            try
            {
                return _storage.InsertAsync(outputs.Select(OutputEntity.Create));
            }
            catch (AggregateException e)
            {
                var exception = e.InnerExceptions[0] as StorageException;
                throwIfBackend(exception);
                throw;
            }
            catch (StorageException e)
            {
                throwIfBackend(e);
                throw;
            }
        }

        public async Task<IEnumerable<IOutput>> GetUnspentOutputs(IEnumerable<IOutput> outputs)
        {
            var enumerable = outputs.ToArray();

            var dbOutputs = await _storage.GetDataAsync(OutputEntity.GeneratePartitionKey(), enumerable.Select(x => OutputEntity.GenerateRowKey(x.TransactionHash, x.N)));

            var setOfSpentRowKeys = new HashSet<string>(dbOutputs.Select(x => x.RowKey));

            return enumerable.Where(x => !setOfSpentRowKeys.Contains(OutputEntity.GenerateRowKey(x.TransactionHash, x.N)));
        }
    }
}

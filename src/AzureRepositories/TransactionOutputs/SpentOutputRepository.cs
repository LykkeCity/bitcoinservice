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

        public Guid TransactionId { get; set; }

        public static OutputEntity Create(Guid transactionId, IOutput output)
        {
            return new OutputEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(output.TransactionHash, output.N),
                TransactionHash = output.TransactionHash,
                N = output.N,
                TransactionId = transactionId
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

        public async Task InsertSpentOutputs(Guid transactionId, IEnumerable<IOutput> outputs)
        {
            Action<StorageException> throwIfBackend = (exception) =>
            {
                if (exception != null && exception.RequestInformation.HttpStatusCode == EntityExistsHttpStatusCode)
                    throw new BackendException("entity already exists", ErrorCode.TransactionConcurrentInputsProblem);
            };

            try
            {
                var forInsert = outputs.Select(o => OutputEntity.Create(transactionId, o)).ToList();

                while (forInsert.Count > 0)
                {
                    var part = forInsert.Take(100);
                    forInsert = forInsert.Skip(100).ToList();
                    await _storage.InsertAsync(part);
                }
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

            var dbOutputs = await _storage.GetDataAsync(OutputEntity.GeneratePartitionKey(), enumerable.Select(x => OutputEntity.GenerateRowKey(x.TransactionHash, x.N)), 50);

            var setOfSpentRowKeys = new HashSet<string>(dbOutputs.Select(x => x.RowKey));

            return enumerable.Where(x => !setOfSpentRowKeys.Contains(OutputEntity.GenerateRowKey(x.TransactionHash, x.N)));
        }

        public async Task RemoveSpentOutputs(IEnumerable<IOutput> outputs)
        {
            var outputEntities = outputs.Select(x => OutputEntity.Create(Guid.NewGuid(), x)).ToList();
            while (outputEntities.Any())
            {
                await _storage.DeleteAsync(outputEntities.Take(50));

                outputEntities = outputEntities.Skip(50).ToList();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Repositories.TransactionOutputs;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoRepositories.Mongo;
using MongoRepositories.Utils;

namespace MongoRepositories.TransactionOutputs
{
    public class OutputEntity : MongoEntity, IOutput
    {

        public static string GenerateId(string hash, int n)
        {
            return $"{hash}_{n}";
        }

        public string TransactionHash { get; set; }
        public int N { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Guid TransactionId { get; set; }

        public static OutputEntity Create(Guid transactionId, IOutput output)
        {
            return new OutputEntity
            {
                BsonId = GenerateId(output.TransactionHash, output.N),
                TransactionHash = output.TransactionHash,
                N = output.N,
                TransactionId = transactionId
            };
        }
    }

    public class SpentOutputRepository : ISpentOutputRepository
    {
        private const int EntityExistsHttpStatusCode = 409;

        private readonly IMongoStorage<OutputEntity> _storage;

        public SpentOutputRepository(IMongoStorage<OutputEntity> storage)
        {
            _storage = storage;
        }

        public async Task InsertSpentOutputs(Guid transactionId, IEnumerable<IOutput> outputs)
        {
            Action<MongoWriteException> throwIfBackend = (exception) =>
            {
                if (exception != null && exception.IsDuplicateError())
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
            catch (MongoWriteException e)
            {
                throwIfBackend(e);
                throw;
            }
        }

        public async Task<IEnumerable<IOutput>> GetUnspentOutputs(IEnumerable<IOutput> outputs)
        {
            var enumerable = outputs.ToArray();
            var ids = enumerable.Select(x => OutputEntity.GenerateId(x.TransactionHash, x.N)).ToArray();

            var hs = new HashSet<string>();

            while (ids.Any())
            {
                var part = ids.Take(200).ToArray();

                var dbOutputs = await _storage.GetDataAsync(o => part.Contains(o.BsonId));

                hs.UnionWith(dbOutputs.Select(x => x.BsonId));

                ids = ids.Skip(200).ToArray();
            }

            return enumerable.Where(x => !hs.Contains(OutputEntity.GenerateId(x.TransactionHash, x.N)));
        }

        public async Task RemoveSpentOutputs(IEnumerable<IOutput> outputs)
        {
            var ids = outputs.Select(x => OutputEntity.GenerateId(x.TransactionHash, x.N)).ToArray();
            await _storage.DeleteAsync(o => ids.Contains(o.BsonId));
        }
    }
}

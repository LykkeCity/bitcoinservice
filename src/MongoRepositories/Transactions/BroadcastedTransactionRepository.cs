using System;
using System.Text;
using System.Threading.Tasks;
using Core.Repositories.Transactions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoRepositories.Mongo;

namespace MongoRepositories.Transactions
{
    public class BroadcastedTransactionEntity : MongoEntity, IBroadcastedTransaction
    {
        
        [BsonIgnore]
        public string Hash => BsonId;

        [BsonRepresentation(BsonType.String)]
        public Guid TransactionId { get; set; }

        public static BroadcastedTransactionEntity Create(string hash, Guid transactionId)
        {
            return new BroadcastedTransactionEntity
            {                
                BsonId = hash,
                TransactionId = transactionId
            };
        }
    }

    public class BroadcastedTransactionRepository : IBroadcastedTransactionRepository
    {
    
        private readonly IMongoStorage<BroadcastedTransactionEntity> _storage;        

        public BroadcastedTransactionRepository(IMongoStorage<BroadcastedTransactionEntity> storage)
        {
            _storage = storage;
        }

        public async Task InsertTransaction(string hash, Guid transactionId)
        {
            await _storage.InsertAsync(BroadcastedTransactionEntity.Create(hash, transactionId));
        }

        public async Task<IBroadcastedTransaction> GetTransaction(string hash)
        {
            return await _storage.GetDataAsync(hash);
        }        
    }
}

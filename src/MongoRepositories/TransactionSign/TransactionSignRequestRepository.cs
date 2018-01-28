using System;
using System.Threading.Tasks;
using Core.Repositories.TransactionSign;
using MongoDB.Bson.Serialization.Attributes;
using MongoRepositories.Mongo;

namespace MongoRepositories.TransactionSign
{
    public class TransactionSignRequestEntity : MongoEntity, ITransactionSignRequest
    {
        [BsonIgnore]
        public Guid TransactionId => Guid.Parse(BsonId);
        public bool? Invalidated { get; set; }
        public string RawRequest { get; set; }
        public bool DoNotSign { get; set; }


        public static TransactionSignRequestEntity Create(Guid transactionId, string rawRequest)
        {
            return new TransactionSignRequestEntity
            {
                BsonId = transactionId.ToString(),                
                RawRequest = rawRequest
            };
        }
    }

    public class TransactionSignRequestRepository : ITransactionSignRequestRepository
    {
        private readonly IMongoStorage<TransactionSignRequestEntity> _table;

        public TransactionSignRequestRepository(IMongoStorage<TransactionSignRequestEntity> table)
        {
            _table = table;
        }

        public async Task<ITransactionSignRequest> GetSignRequest(Guid transactionId)
        {
            return await _table.GetDataAsync(transactionId.ToString());
        }

        public async Task<Guid> InsertTransactionId(Guid? transactionId, string rawRequest)
        {
            var guid = transactionId ?? Guid.NewGuid();
            await _table.InsertOrReplaceAsync(TransactionSignRequestEntity.Create(guid, rawRequest));
            return guid;
        }

        public async Task InvalidateTransactionId(Guid transactionId)
        {
            await _table.ReplaceAsync(transactionId.ToString(),
                entity =>
                {
                    entity.Invalidated = true;
                    return entity;
                });
        }

        public Task DoNotSign(Guid transactionId)
        {
            return _table.ReplaceAsync(transactionId.ToString(),
                entity =>
                {
                    entity.DoNotSign = true;
                    return entity;
                });
        }
    }
}

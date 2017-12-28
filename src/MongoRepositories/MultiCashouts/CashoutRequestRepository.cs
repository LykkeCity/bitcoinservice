using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.Repositories.MultipleCashouts;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoRepositories.Mongo;

namespace MongoRepositories.MultiCashouts
{
    public class CashoutRequestEntity : MongoEntity, ICashoutRequest
    {
        [BsonIgnore]
        public Guid CashoutRequestId => Guid.Parse(BsonId);
        public decimal Amount { get; set; }
        public string DestinationAddress { get; set; }        
        public DateTime Date { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Guid? MultipleCashoutId { get; set; }

        public static CashoutRequestEntity Create(Guid id, decimal amount, string destination)
        {
            return new CashoutRequestEntity
            {
                BsonId = id.ToString(),
                Amount = amount,
                DestinationAddress = destination,
                Date = DateTime.UtcNow
            };
        }
    }

    public class CashoutRequestRepository : ICashoutRequestRepository
    {
        private readonly IMongoStorage<CashoutRequestEntity> _table;

        public CashoutRequestRepository(IMongoStorage<CashoutRequestEntity> table)
        {
            _table = table;
        }

        public async Task<ICashoutRequest> CreateCashoutRequest(Guid id, decimal amount, string destination)
        {
            var entity = CashoutRequestEntity.Create(id, amount, destination);
            await _table.InsertAsync(entity);
            return entity;
        }

        public async Task<IEnumerable<ICashoutRequest>> GetOpenRequests()
        {
            return await _table.GetDataAsync(o => o.MultipleCashoutId == null);
        }

        public async Task<IEnumerable<ICashoutRequest>> GetCashoutRequests(Guid multipleCashoutId)
        {
            return await _table.GetDataAsync(o => o.MultipleCashoutId == multipleCashoutId);
        }

        public async Task SetMultiCashoutId(IEnumerable<Guid> cashoutIds, Guid multiCashoutId)
        {
            foreach (var cashoutId in cashoutIds)
            {
                await _table.ReplaceAsync(cashoutId.ToString(), entity =>
                {
                    entity.MultipleCashoutId = multiCashoutId;
                    return entity;
                });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Repositories.MultipleCashouts;
using MongoDB.Bson.Serialization.Attributes;
using MongoRepositories.Mongo;

namespace MongoRepositories.MultiCashouts
{
    public class MultiCashoutEntity : MongoEntity, IMultipleCashout
    {
        [BsonIgnore]
        public Guid MultipleCashoutId => Guid.Parse(BsonId);
        public string TransactionHash { get; set; }
        public string TransactionHex { get; set; }
        public int TryCount { get; set; }

        public MultiCashoutState State { get; set; }

        public static MultiCashoutEntity Create(Guid id, string hex, string hash)
        {
            return new MultiCashoutEntity
            {
                BsonId = id.ToString(),
                TransactionHex = hex,
                TryCount = 0,
                TransactionHash = hash,
                State = MultiCashoutState.Open
            };
        }
    }


    public class MultiCashoutRepository: IMultiCashoutRepository
    {
        private readonly IMongoStorage<MultiCashoutEntity> _table;

        public MultiCashoutRepository(IMongoStorage<MultiCashoutEntity> table)
        {
            _table = table;
        }

        public async Task<IMultipleCashout> GetCurrentMultiCashout()
        {
            return (await _table.GetDataAsync(o => o.State == MultiCashoutState.Open)).FirstOrDefault();
        }

        public Task CloseMultiCashout(Guid multicashoutId)
        {
            return _table.ReplaceAsync(multicashoutId.ToString(), entity =>
            {
                entity.State = MultiCashoutState.Closed;
                return entity;
            });
        }

        public Task CompleteMultiCashout(Guid multicashoutId)
        {
            return _table.ReplaceAsync(multicashoutId.ToString(), entity =>
            {
                entity.State = MultiCashoutState.Completed;
                return entity;
            });
        }

        public Task CreateMultiCashout(Guid multiCashoutId, string hex, string txHash)
        {
            return _table.InsertAsync(MultiCashoutEntity.Create(multiCashoutId, hex, txHash));
        }

        public Task IncreaseTryCount(Guid multiCashoutId)
        {
            return _table.ReplaceAsync(multiCashoutId.ToString(), entity =>
            {
                entity.TryCount++;
                return entity;
            });
        }
    }
}

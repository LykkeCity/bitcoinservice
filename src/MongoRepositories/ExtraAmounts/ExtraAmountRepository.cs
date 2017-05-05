using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Repositories.ExtraAmounts;
using MongoDB.Bson.Serialization.Attributes;
using MongoRepositories.Mongo;

namespace MongoRepositories.ExtraAmounts
{

    public class ExtraAmountEntity : MongoEntity, IExtraAmount
    {
        public static string Partition = "ExtraAmount";

        [BsonIgnore]
        public string Address => BsonId;

        public long Amount { get; set; }

        public static ExtraAmountEntity Create(string address, long amount)
        {
            return new ExtraAmountEntity
            {
                BsonId = address,
                Amount = amount
            };
        }
    }



    public class ExtraAmountRepository : IExtraAmountRepository
    {
        private readonly IMongoStorage<ExtraAmountEntity> _table;

        public ExtraAmountRepository(IMongoStorage<ExtraAmountEntity> table)
        {
            _table = table;
        }

        public async Task<IExtraAmount> Add(string address, long amount)
        {
            var entity = ExtraAmountEntity.Create(address, amount);

            await _table.InsertOrModifyAsync(address, () => entity, x =>
            {
                x.Amount += amount;
                return x;
            });

            return entity;
        }

        public Task Decrease(IExtraAmount extraAmount)
        {
            return _table.ReplaceAsync(extraAmount.Address, amountEntity =>
           {
               amountEntity.Amount -= extraAmount.Amount;
               return amountEntity;
           });
        }

        public async Task<IEnumerable<IExtraAmount>> GetData()
        {
            return await _table.GetDataAsync();
        }
    }
}

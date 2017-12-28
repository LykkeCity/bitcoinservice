using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.Repositories.PaidFees;
using MongoRepositories.Mongo;

namespace MongoRepositories.PaidFees
{
    public class PaidFeesEntity : MongoEntity, IPaidFees
    {
        public string TransactionHash { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Multisig { get; set; }
        public string Asset { get; set; }

        public static PaidFeesEntity Create(string hash, decimal amount, DateTime date, string multisig, string asset)
        {
            return new PaidFeesEntity
            {
                BsonId = hash,
                TransactionHash = hash,
                Amount = amount,
                Date = date,
                Multisig = multisig,
                Asset = asset
            };
        }
    }


    public class PaidFeesRepository : IPaidFeesRepository
    {
        private readonly IMongoStorage<PaidFeesEntity> _table;

        public PaidFeesRepository(IMongoStorage<PaidFeesEntity> table)
        {
            _table = table;
        }

        public Task Insert(string hash, decimal amount, DateTime date, string multisig, string asset)
        {
            return _table.InsertOrReplaceAsync(PaidFeesEntity.Create(hash, amount, date, multisig, asset));
        }
    }
}

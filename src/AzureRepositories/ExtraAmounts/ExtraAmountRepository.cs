using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.ExtraAmounts;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.ExtraAmounts
{

    public class ExtraAmountEntity : TableEntity, IExtraAmount
    {
        public static string Partition = "ExtraAmount";

        public string Address => RowKey;

        public long Amount { get; set; }

        public static ExtraAmountEntity Create(string address, long amount)
        {
            return new ExtraAmountEntity
            {
                PartitionKey = Partition,
                RowKey = address,
                Amount = amount
            };
        }
    }



    public class ExtraAmountRepository : IExtraAmountRepository
    {
        private readonly INoSQLTableStorage<ExtraAmountEntity> _table;

        public ExtraAmountRepository(INoSQLTableStorage<ExtraAmountEntity> table)
        {
            _table = table;
        }

        public async Task<IExtraAmount> Add(string address, long amount)
        {
            var entity = ExtraAmountEntity.Create(address, amount);

            await _table.InsertOrModifyAsync(ExtraAmountEntity.Partition, address, () => entity, x =>
            {
                x.Amount += amount;
                return x;
            });

            return entity;
        }

        public Task Decrease(IExtraAmount extraAmount)
        {
            return _table.ReplaceAsync(ExtraAmountEntity.Partition, extraAmount.Address, amountEntity =>
           {
               amountEntity.Amount -= extraAmount.Amount;
               return amountEntity;
           });
        }
    }
}

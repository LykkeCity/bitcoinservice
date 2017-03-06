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

        public string Address { get; set; }

        public long Amount { get; set; }

        public static ExtraAmountEntity Create(string address, long amount)
        {
            return new ExtraAmountEntity
            {
                PartitionKey = Partition,
                RowKey = Guid.NewGuid().ToString(),
                Address = address,
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

        public async Task<Guid> Add(string address, long amount)
        {
            var entity = ExtraAmountEntity.Create(address, amount);
            await _table.InsertAsync(entity);
            return Guid.Parse(entity.RowKey);
        }

        public Task Remove(Guid id)
        {
            return _table.DeleteAsync(ExtraAmountEntity.Partition, id.ToString());
        }
    }
}

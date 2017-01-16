using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.Offchain;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Offchain
{
    public class OffchainChannelEntity : TableEntity, IOffchainChannel
    {
        public Guid TransactionId { get; set; }
        public string Multisig { get; set; }
        public string Asset { get; set; }
        public string InitialTransaction { get; set; }

        public decimal ClientAmount { get; set; }

        public decimal HubAmount { get; set; }

        public string FullySignedChannel { get; set; }


        public static string GeneratePartitionKey(string asset)
        {
            return asset;
        }

        public static string GenerateRowKey(string multisig)
        {
            return multisig;
        }

        public static OffchainChannelEntity Create(Guid transactionId, string multisig, string asset, string initialTr)
        {
            return new OffchainChannelEntity
            {                
                PartitionKey = GeneratePartitionKey(asset),
                RowKey = GenerateRowKey(multisig),
                TransactionId = transactionId,
                Asset = asset,
                Multisig = multisig,
                InitialTransaction = initialTr
            };
        }
        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);

            foreach (var p in GetType().GetProperties().Where(x => x.PropertyType == typeof(decimal) && properties.ContainsKey(x.Name)))
                p.SetValue(this, Convert.ToDecimal(properties[p.Name].StringValue));
        }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var properties = base.WriteEntity(operationContext);

            foreach (var p in GetType().GetProperties().Where(x => x.PropertyType == typeof(decimal)))
                properties.Add(p.Name, new EntityProperty(p.GetValue(this).ToString()));

            return properties;
        }
    }


    public class OffchainChannelRepository : IOffchainChannelRepository
    {
        private readonly INoSQLTableStorage<OffchainChannelEntity> _table;

        public OffchainChannelRepository(INoSQLTableStorage<OffchainChannelEntity> table)
        {
            _table = table;
        }

        public async Task<IOffchainChannel> CreateChannel(Guid transactionId, string multisig, string asset, string initialTr)
        {
            var entity = OffchainChannelEntity.Create(transactionId, multisig, asset, initialTr);
            await _table.InsertOrReplaceAsync(entity);
            return entity;
        }

        public async Task<IOffchainChannel> GetChannel(string multisig, string assetName)
        {
            return await _table.GetDataAsync(OffchainChannelEntity.GeneratePartitionKey(assetName),
                OffchainChannelEntity.GenerateRowKey(multisig));
        }

        public async Task SetFullSignedTransactionAndAmount(string multisig, string assetName, string fullSignedTr, decimal hubAmount,
            decimal clientAmount)
        {
            await _table.ReplaceAsync(OffchainChannelEntity.GeneratePartitionKey(assetName),
               OffchainChannelEntity.GenerateRowKey(multisig), entity =>
                {
                    entity.FullySignedChannel = fullSignedTr;
                    entity.HubAmount = hubAmount;
                    entity.ClientAmount = clientAmount;
                    return entity;
                });
        }
    }
}

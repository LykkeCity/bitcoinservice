using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Core.Repositories.Offchain;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Offchain
{
    public class OffchainChannelEntity : BaseEntity, IOffchainChannel
    {
        public Guid TransactionId { get; set; }
        public string Multisig { get; set; }
        public string Asset { get; set; }
        public string InitialTransaction { get; set; }

        public decimal ClientAmount { get; set; }

        public decimal HubAmount { get; set; }

        public string FullySignedChannel { get; set; }

        public bool IsBroadcasted { get; set; }

        public class CurrentChannel
        {
            public static string GeneratePartitionKey(string asset)
            {
                return asset;
            }

            public static string GenerateRowKey(string multisig)
            {
                return multisig;
            }

            public static OffchainChannelEntity Create(string multisig, string asset,
                string initialTr, decimal clientAmount, decimal hubAmount)
            {
                return new OffchainChannelEntity
                {
                    PartitionKey = GeneratePartitionKey(asset),
                    RowKey = GenerateRowKey(multisig),
                    TransactionId = Guid.NewGuid(),
                    Asset = asset,
                    Multisig = multisig,
                    InitialTransaction = initialTr,
                    HubAmount = hubAmount,
                    ClientAmount = clientAmount,
                    FullySignedChannel = null
                };
            }
        }

        public class Archived
        {
            public static string GeneratePartitionKey(string asset, string multisig)
            {
                return asset + "_" + multisig;
            }

            public static OffchainChannelEntity Create(IOffchainChannel channel)
            {
                return new OffchainChannelEntity
                {
                    PartitionKey = GeneratePartitionKey(channel.Asset, channel.Multisig),
                    RowKey = Guid.NewGuid().ToString(),
                    TransactionId = channel.TransactionId,
                    Asset = channel.Asset,
                    Multisig = channel.Multisig,
                    InitialTransaction = channel.InitialTransaction,
                    HubAmount = channel.HubAmount,
                    ClientAmount = channel.ClientAmount,
                    FullySignedChannel = channel.FullySignedChannel
                };
            }
        }
    }


    public class OffchainChannelRepository : IOffchainChannelRepository
    {
        private readonly INoSQLTableStorage<OffchainChannelEntity> _table;

        public OffchainChannelRepository(INoSQLTableStorage<OffchainChannelEntity> table)
        {
            _table = table;
        }

        public async Task<IOffchainChannel> CreateChannel(string multisig, string asset, string initialTr, decimal clientAmount, decimal hubAmount)
        {
            var current = await GetChannel(multisig, asset);

            if (current != null)
            {
                var archived = OffchainChannelEntity.Archived.Create(current);
                await _table.InsertAsync(archived);
            }

            var newChannel = OffchainChannelEntity.CurrentChannel.Create(multisig, asset, initialTr, clientAmount, hubAmount);
            await _table.InsertOrReplaceAsync(newChannel);

            return newChannel;
        }

        public async Task<IOffchainChannel> GetChannel(string multisig, string asset)
        {
            return await _table.GetDataAsync(OffchainChannelEntity.CurrentChannel.GeneratePartitionKey(asset),
                OffchainChannelEntity.CurrentChannel.GenerateRowKey(multisig));
        }

        public async Task<IOffchainChannel> SetFullSignedTransaction(string multisig, string asset, string fullSignedTr)
        {
            return await _table.ReplaceAsync(OffchainChannelEntity.CurrentChannel.GeneratePartitionKey(asset),
               OffchainChannelEntity.CurrentChannel.GenerateRowKey(multisig), entity =>
                {
                    entity.FullySignedChannel = fullSignedTr;
                    return entity;
                });
        }

        public async Task UpdateAmounts(string multisig, string asset, decimal clientAmount, decimal hubAmount)
        {
            await _table.ReplaceAsync(OffchainChannelEntity.CurrentChannel.GeneratePartitionKey(asset),
              OffchainChannelEntity.CurrentChannel.GenerateRowKey(multisig), entity =>
                {
                    entity.ClientAmount = clientAmount;
                    entity.HubAmount = hubAmount;
                    return entity;
                });
        }

        public Task SetChannelBroadcasted(string multisig, string asset)
        {
            return _table.ReplaceAsync(OffchainChannelEntity.CurrentChannel.GeneratePartitionKey(asset),
             OffchainChannelEntity.CurrentChannel.GenerateRowKey(multisig), entity =>
                {
                    entity.IsBroadcasted = true;
                 return entity;
             });
        }
    }
}

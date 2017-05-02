using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.Offchain;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Offchain
{
    public class ClosingChannelEntity : TableEntity, IClosingChannel
    {
        public Guid ClosingChannelId { get; set; }
        public string Multisig { get; set; }
        public string Asset { get; set; }
        public string InitialTransaction { get; set; }
        public Guid ChannelId { get; set; }

        public static class Current
        {
            public static string GeneratePartitionKey(string asset)
            {
                return asset;
            }

            public static string GenerateRowKey(string multisig)
            {
                return multisig;
            }

            public static ClosingChannelEntity Create(string multisig, string asset, Guid channelId, string initialTransaction)
            {
                return new ClosingChannelEntity
                {
                    PartitionKey = GeneratePartitionKey(asset),
                    RowKey = GenerateRowKey(multisig),
                    ClosingChannelId = Guid.NewGuid(),
                    Multisig = multisig,
                    Asset = asset,
                    ChannelId = channelId,
                    InitialTransaction = initialTransaction
                };
            }

        }

        public static class Archived
        {
            public static string GeneratePartition()
            {
                return "Archived";
            }

            public static ClosingChannelEntity Create(IClosingChannel closingChannel)
            {
                return new ClosingChannelEntity
                {
                    PartitionKey = GeneratePartition(),
                    RowKey = closingChannel.ClosingChannelId.ToString(),
                    Multisig = closingChannel.Multisig,
                    Asset = closingChannel.Asset,
                    ChannelId = closingChannel.ChannelId,
                    ClosingChannelId = closingChannel.ClosingChannelId,
                    InitialTransaction = closingChannel.InitialTransaction
                };
            }
        }
    }



    public class ClosingChannelRepository : IClosingChannelRepository
    {
        private readonly INoSQLTableStorage<ClosingChannelEntity> _table;

        public ClosingChannelRepository(INoSQLTableStorage<ClosingChannelEntity> table)
        {
            _table = table;
        }

        public async Task<IClosingChannel> GetClosingChannel(string multisig, string asset)
        {
            return await _table.GetDataAsync(ClosingChannelEntity.Current.GeneratePartitionKey(asset),
                        ClosingChannelEntity.Current.GenerateRowKey(multisig));
        }

        public async Task<IClosingChannel> CreateClosingChannel(string multisig, string asset, Guid channelId, string initialTransaction)
        {
            var closing = await GetClosingChannel(multisig, asset);
            if (closing != null)
            {
                await CompleteClosingChannel(multisig, asset, closing.ClosingChannelId);
            }
            var entity = ClosingChannelEntity.Current.Create(multisig, asset, channelId, initialTransaction);
            await _table.InsertAsync(entity);
            return entity;
        }

        public async Task CompleteClosingChannel(string multisig, string asset, Guid closingChannelId)
        {
            var closing = await GetClosingChannel(multisig, asset);
            if (closing?.ClosingChannelId == closingChannelId)
            {
                await Task.WhenAll(
                    _table.InsertAsync(ClosingChannelEntity.Archived.Create(closing)),
                    _table.DeleteAsync((ClosingChannelEntity)closing)
                );
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using Core.Repositories.Offchain;
using MongoRepositories.Mongo;

namespace MongoRepositories.Offchain
{
    public class OffchainChannelEntity : MongoEntity, IOffchainChannel
    {
        public Guid ChannelId { get; set; }
        public string Multisig { get; set; }
        public string Asset { get; set; }
        public string InitialTransaction { get; set; }

        public decimal ClientAmount { get; set; }

        public decimal HubAmount { get; set; }

        public string FullySignedChannel { get; set; }

        public bool IsBroadcasted { get; set; }

        public Guid? PrevChannelTransactionId { get; set; }

        public bool Actual { get; set; }

        public class CurrentChannel
        {
            public static string GenerateId(string multisig, string asset)
            {
                return asset + "_" + multisig;
            }

            public static OffchainChannelEntity Create(string multisig, string asset,
                string initialTr, decimal clientAmount, decimal hubAmount, Guid? prevChannelTransactionId)
            {
                return new OffchainChannelEntity
                {
                    BsonId = GenerateId(multisig, asset),
                    ChannelId = Guid.NewGuid(),
                    Asset = asset,
                    Multisig = multisig,
                    InitialTransaction = initialTr,
                    HubAmount = hubAmount,
                    ClientAmount = clientAmount,
                    FullySignedChannel = null,
                    PrevChannelTransactionId = prevChannelTransactionId,
                    Actual = true
                };
            }

            public static OffchainChannelEntity Create(IOffchainChannel channel)
            {
                return new OffchainChannelEntity
                {
                    BsonId = GenerateId(channel.Multisig, channel.Asset),
                    ChannelId = channel.ChannelId,
                    Asset = channel.Asset,
                    Multisig = channel.Multisig,
                    InitialTransaction = channel.InitialTransaction,
                    HubAmount = channel.HubAmount,
                    ClientAmount = channel.ClientAmount,
                    FullySignedChannel = channel.FullySignedChannel,
                    IsBroadcasted = channel.IsBroadcasted,
                    PrevChannelTransactionId = channel.PrevChannelTransactionId,
                    Actual = true
                };
            }
        }

        public class Archived
        {

            public static OffchainChannelEntity Create(IOffchainChannel channel)
            {
                return new OffchainChannelEntity
                {
                    BsonId = channel.ChannelId.ToString(),
                    ChannelId = channel.ChannelId,
                    Asset = channel.Asset,
                    Multisig = channel.Multisig,
                    InitialTransaction = channel.InitialTransaction,
                    HubAmount = channel.HubAmount,
                    ClientAmount = channel.ClientAmount,
                    FullySignedChannel = channel.FullySignedChannel,
                    IsBroadcasted = channel.IsBroadcasted,
                    PrevChannelTransactionId = channel.PrevChannelTransactionId,
                    Actual = false
                };
            }
        }
    }


    public class OffchainChannelRepository : IOffchainChannelRepository
    {
        private readonly IMongoStorage<OffchainChannelEntity> _table;

        public OffchainChannelRepository(IMongoStorage<OffchainChannelEntity> table)
        {
            _table = table;
        }

        public async Task<IOffchainChannel> CreateChannel(string multisig, string asset, string initialTr, decimal clientAmount, decimal hubAmount)
        {
            var current = await GetChannel(multisig, asset);

            if (current != null)
            {
                await CloseChannel(current.Multisig, current.Asset, current.ChannelId);
            }

            var newChannel = OffchainChannelEntity.CurrentChannel.Create(multisig, asset, initialTr, clientAmount, hubAmount, current?.ChannelId);
            await _table.InsertAsync(newChannel);

            return newChannel;
        }

        public async Task<IOffchainChannel> GetChannel(string multisig, string asset)
        {
            return await _table.GetDataAsync(OffchainChannelEntity.CurrentChannel.GenerateId(multisig, asset));
        }

        public async Task<IOffchainChannel> SetFullSignedTransaction(string multisig, string asset, string fullSignedTr)
        {
            return await _table.ReplaceAsync(OffchainChannelEntity.CurrentChannel.GenerateId(multisig, asset), entity =>
                {
                    entity.FullySignedChannel = fullSignedTr;
                    return entity;
                });
        }

        public async Task UpdateAmounts(string multisig, string asset, decimal clientAmount, decimal hubAmount)
        {
            await _table.ReplaceAsync(OffchainChannelEntity.CurrentChannel.GenerateId(multisig, asset), entity =>
                {
                    entity.ClientAmount = clientAmount;
                    entity.HubAmount = hubAmount;
                    return entity;
                });
        }

        public Task SetChannelBroadcasted(string multisig, string asset)
        {
            return _table.ReplaceAsync(OffchainChannelEntity.CurrentChannel.GenerateId(multisig, asset), entity =>
                {
                    entity.IsBroadcasted = true;
                    return entity;
                });
        }

        public async Task CloseChannel(string multisig, string asset, Guid channelId)
        {
            var current = (OffchainChannelEntity)await GetChannel(multisig, asset);

            if (current != null && current.ChannelId == channelId)
            {
                var archived = OffchainChannelEntity.Archived.Create(current);
                await _table.InsertAsync(archived);
                await _table.DeleteAsync(current);
            }
        }

        public async Task RevertChannel(string multisig, string asset, Guid channelId)
        {
            var current = (OffchainChannelEntity)await GetChannel(multisig, asset);
            if (current != null && current.ChannelId == channelId)
            {
                await _table.DeleteAsync(current);
                if (!current.PrevChannelTransactionId.HasValue)
                    return;
                var archived = await _table.GetDataAsync(current.PrevChannelTransactionId.ToString());
                if (archived != null)
                {
                    await _table.DeleteAsync(archived.ChannelId.ToString());
                    var entity = OffchainChannelEntity.CurrentChannel.Create(archived);
                    await _table.InsertAsync(entity);
                }
            }
        }
    }
}

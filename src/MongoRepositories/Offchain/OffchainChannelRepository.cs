using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Core.Repositories.Offchain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoRepositories.Mongo;

namespace MongoRepositories.Offchain
{
    public class OffchainChannelEntity : MongoEntity, IOffchainChannel
    {
        [BsonRepresentation(BsonType.String)]
        public Guid ChannelId { get; set; }
        public string Multisig { get; set; }
        public string Asset { get; set; }
        public string InitialTransaction { get; set; }

        public decimal ClientAmount { get; set; }

        public decimal HubAmount { get; set; }

        public string FullySignedChannel { get; set; }

        public bool IsBroadcasted { get; set; }

        public DateTime CreateDt { get; set; }

        [BsonRepresentation(BsonType.String)]
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
                    CreateDt = DateTime.UtcNow,
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
                    Actual = true,
                    CreateDt = channel.CreateDt
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
                    CreateDt = channel.CreateDt,
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

        public async Task<IOffchainChannel> GetLastChannel(string multisig, string asset)
        {
            return await _table.GetTopRecordAsync(o => o.Multisig == multisig && o.Asset == asset, o => o.CreateDt, SortDirection.Descending);
        }

        public async Task<IEnumerable<IOffchainChannel>> GetChannels(string multisig, string asett)
        {
            return await _table.GetDataAsync(o => o.Multisig == multisig && o.Asset == asett);
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

        public Task<bool> HasChannel(string multisig)
        {
            return _table.Any(o => o.Multisig == multisig);
        }

        public async Task<IEnumerable<IOffchainChannel>> GetChannels(string asset)
        {
            return await _table.GetDataAsync(o => o.Asset == asset && o.Actual);
        }

        public async Task<IEnumerable<IOffchainChannel>> GetAllChannels(string asset)
        {
            return await _table.GetDataAsync(o => o.Asset == asset);
        }

        public async Task<IEnumerable<IOffchainChannel>> GetAllChannelsByDate(string asset, DateTime date)
        {
            return await _table.GetDataAsync(o => o.Asset == asset && o.CreateDt <= date && o.IsBroadcasted &&
                                                  (o.Actual || o.BsonCreateDt > date));
        }

        public async Task<IEnumerable<IOffchainChannel>> GetChannelsOfMultisig(string multisig)
        {
            return await _table.GetDataAsync(o => o.Multisig == multisig && o.Actual);
        }
    }
}

using System;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Repositories.Offchain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoRepositories.Mongo;
using MongoRepositories.Utils;

namespace MongoRepositories.Offchain
{
    public class OffchainTransferEntity : MongoEntity, IOffchainTransfer
    {
        [BsonRepresentation(BsonType.String)]
        public Guid TransferId { get; set; }
        public string Multisig { get; set; }
        public string AssetId { get; set; }

        public bool Completed { get; set; }

        public bool Required { get; set; }
        public bool Closed { get; set; }

        public DateTime CreateDt { get; set; }


        public static class ByRecord
        {

            public static string GenerateId(string multisig, string asset)
            {
                return asset + "_" + multisig;
            }

            public static OffchainTransferEntity Create(string multisig, string asset, bool required)
            {
                return new OffchainTransferEntity
                {
                    BsonId = GenerateId(multisig, asset),
                    TransferId = Guid.NewGuid(),
                    CreateDt = DateTime.UtcNow,
                    AssetId = asset,
                    Multisig = multisig,
                    Required = required
                };
            }
        }

        public static class Archive
        {
            public static OffchainTransferEntity Create(IOffchainTransfer transfer)
            {
                return new OffchainTransferEntity
                {
                    BsonId = transfer.TransferId.ToString(),
                    TransferId = transfer.TransferId,
                    CreateDt = transfer.CreateDt,
                    Required = transfer.Required,
                    AssetId = transfer.AssetId,
                    Multisig = transfer.Multisig,
                    Completed = transfer.Completed,
                    Closed = transfer.Closed
                };
            }
        }
    }


    public class OffchainTransferRepository : IOffchainTransferRepository
    {
        private readonly IMongoStorage<OffchainTransferEntity> _table;

        public OffchainTransferRepository(IMongoStorage<OffchainTransferEntity> table)
        {
            _table = table;
        }

        public async Task<IOffchainTransfer> CreateTransfer(string multisig, string asset, bool required)
        {
            Action<MongoWriteException> throwIfBackend = (exception) =>
            {
                if (exception != null && exception.IsDuplicateError())
                    throw new BackendException("entity already exists", ErrorCode.DuplicateRequest);
            };

            try
            {
                var entity = OffchainTransferEntity.ByRecord.Create(multisig, asset, required);
                await _table.InsertAsync(entity);
                return entity;
            }
            catch (MongoWriteException e)
            {
                throwIfBackend(e);
                throw;
            }
        }


        public async Task<IOffchainTransfer> GetLastTransfer(string multisig, string assetId)
        {
            return await _table.GetDataAsync(OffchainTransferEntity.ByRecord.GenerateId(multisig, assetId));
        }

        public async Task CompleteTransfer(string multisig, string asset, Guid transferId)
        {
            var entity = await _table.GetDataAsync(OffchainTransferEntity.ByRecord.GenerateId(multisig, asset));
            if (entity?.TransferId == transferId)
            {
                var archive = OffchainTransferEntity.Archive.Create(entity);
                archive.Completed = true;
                await Task.WhenAll(_table.InsertAsync(archive), _table.DeleteAsync(entity));
            }
        }

        public async Task CloseTransfer(string multisig, string asset, Guid transferId)
        {
            var entity = await _table.GetDataAsync(OffchainTransferEntity.ByRecord.GenerateId(multisig, asset));
            if (entity?.TransferId == transferId)
            {
                var archive = OffchainTransferEntity.Archive.Create(entity);
                archive.Closed = true;
                await Task.WhenAll(_table.InsertAsync(archive), _table.DeleteAsync(entity));
            }
        }
    }
}

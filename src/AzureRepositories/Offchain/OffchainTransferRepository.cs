using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.Offchain;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Offchain
{
    public class OffchainTransferEntity : TableEntity, IOffchainTransfer
    {

        public Guid TransferId => Guid.Parse(RowKey);
        public string Multisig { get; set; }
        public string AssetId { get; set; }

        public bool Completed { get; set; }

        public bool Required { get; set; }
        public bool Closed { get; set; }

        public DateTime CreateDt { get; set; }


        public static class ByRecord
        {

            public static string GeneratePartition(string multisig, string asset)
            {
                return asset + "_" + multisig;
            }

            public static OffchainTransferEntity Create(string multisig, string asset, bool required)
            {
                return new OffchainTransferEntity
                {
                    RowKey = Guid.NewGuid().ToString(),
                    PartitionKey = GeneratePartition(multisig, asset),
                    CreateDt = DateTime.UtcNow,
                    AssetId = asset,
                    Multisig = multisig,
                    Required = required
                };
            }
        }

        public static class Archive
        {
            public static string GeneratePartition()
            {
                return "Archive";
            }

            public static OffchainTransferEntity Create(IOffchainTransfer transfer)
            {
                return new OffchainTransferEntity
                {
                    RowKey = transfer.TransferId.ToString(),
                    PartitionKey = GeneratePartition(),
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
        private readonly INoSQLTableStorage<OffchainTransferEntity> _table;

        public OffchainTransferRepository(INoSQLTableStorage<OffchainTransferEntity> table)
        {
            _table = table;
        }

        public async Task<IOffchainTransfer> CreateTransfer(string multisig, string asset, bool required)
        {
            var entity = OffchainTransferEntity.ByRecord.Create(multisig, asset, required);
            await _table.InsertAsync(entity);
            return entity;
        }

        public async Task<IOffchainTransfer> GetTransfer(string multisig, string asset, Guid transferId)
        {
            return await _table.GetDataAsync(OffchainTransferEntity.ByRecord.GeneratePartition(multisig, asset), transferId.ToString());
        }

        public async Task<IOffchainTransfer> GetLastTransfer(string multisig, string assetId)
        {
            return (await _table.GetDataAsync(OffchainTransferEntity.ByRecord.GeneratePartition(multisig, assetId))).OrderByDescending(o => o.CreateDt)
                    .FirstOrDefault();
        }

        public async Task CompleteTransfer(string multisig, string asset, Guid transferId)
        {

            var entity = await _table.GetDataAsync(OffchainTransferEntity.ByRecord.GeneratePartition(multisig, asset), transferId.ToString());
            if (entity != null)
            {
                var archive = OffchainTransferEntity.Archive.Create(entity);
                archive.Completed = true;
                await _table.InsertAsync(archive);
                await _table.DeleteAsync(entity);
            }            
        }

        public async Task CloseTransfer(string multisig, string asset, Guid transferId)
        {
            var entity = await _table.GetDataAsync(OffchainTransferEntity.ByRecord.GeneratePartition(multisig, asset), transferId.ToString());
            if (entity != null)
            {
                var archive = OffchainTransferEntity.Archive.Create(entity);
                archive.Closed = true;
                await _table.InsertAsync(archive);
                await _table.DeleteAsync(entity);
            }
        }
    }
}

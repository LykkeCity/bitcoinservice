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
                Required = required
            };
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
            var entity = OffchainTransferEntity.Create(multisig, asset, required);
            await _table.InsertAsync(entity);
            return entity;
        }

        public async Task<IOffchainTransfer> GetTransfer(string multisig, string asset, Guid transferId)
        {
            return await _table.GetDataAsync(OffchainTransferEntity.GeneratePartition(multisig, asset), transferId.ToString());
        }

        public async Task<IOffchainTransfer> GetLastTransfer(string multisig, string assetId)
        {
            return (await _table.GetDataAsync(OffchainTransferEntity.GeneratePartition(multisig, assetId))).Where(o => !o.Closed).OrderByDescending(o => o.CreateDt)
                    .FirstOrDefault();
        }

        public Task CompleteTransfer(string multisig, string asset, Guid transferId)
        {
            return _table.ReplaceAsync(OffchainTransferEntity.GeneratePartition(multisig, asset), transferId.ToString(), entity =>
            {
                entity.Completed = true;
                return entity;
            });
        }

        public Task CloseTransfer(string multisig, string asset, Guid transferId)
        {
            return _table.ReplaceAsync(OffchainTransferEntity.GeneratePartition(multisig, asset), transferId.ToString(), entity =>
            {
                entity.Closed = true;
                return entity;
            });
        }
    }
}

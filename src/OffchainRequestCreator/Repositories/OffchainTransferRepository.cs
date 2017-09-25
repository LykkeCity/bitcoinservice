using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AzureRepositories;
using AzureStorage;
using Microsoft.WindowsAzure.Storage.Table;

namespace OffchainRequestCreator.Repositories
{
    public class OffchainTransferEntity : BaseEntity, IOffchainTransfer
    {
        public string Id => RowKey;
        public string ClientId { get; set; }
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
        public bool Completed { get; set; }
        public string OrderId { get; set; }
        public DateTime CreatedDt { get; set; }
        public string ExternalTransferId { get; set; }
        public OffchainTransferType Type { get; set; }
        public bool ChannelClosing { get; set; }
        public bool Onchain { get; set; }
        public bool IsChild { get; set; }
        public string ParentTransferId { get; set; }
        public string AdditionalDataJson { get; set; }

        public class ByCommon
        {
            public static string GeneratePartitionKey()
            {
                return "OffchainTransfer";
            }

            public static OffchainTransferEntity Create(string id, string clientId, string assetId, decimal amount, OffchainTransferType type, string externalTransferId,
                string orderId = null, bool channelClosing = false, bool onchain = false)
            {
                return new OffchainTransferEntity
                {
                    PartitionKey = GeneratePartitionKey(),
                    RowKey = id,
                    AssetId = assetId,
                    Amount = amount,
                    ClientId = clientId,
                    OrderId = orderId,
                    CreatedDt = DateTime.UtcNow,
                    ExternalTransferId = externalTransferId,
                    Type = type,
                    ChannelClosing = channelClosing,
                    Onchain = onchain
                };
            }
        }
    }

    public class OffchainTransferRepository : IOffchainTransferRepository
    {
        private readonly INoSQLTableStorage<OffchainTransferEntity> _storage;

        public OffchainTransferRepository(INoSQLTableStorage<OffchainTransferEntity> storage)
        {
            _storage = storage;
        }

        public async Task<IOffchainTransfer> CreateTransfer(string transactionId, string clientId, string assetId, decimal amount, OffchainTransferType type, string externalTransferId, string orderId, bool channelClosing = false)
        {
            var entity = OffchainTransferEntity.ByCommon.Create(transactionId, clientId, assetId, amount, type, externalTransferId, orderId, channelClosing);

            await _storage.InsertAsync(entity);

            return entity;
        }

        public async Task<IOffchainTransfer> GetTransfer(string id)
        {
            return await _storage.GetDataAsync(OffchainTransferEntity.ByCommon.GeneratePartitionKey(), id);
        }

        public async Task CompleteTransfer(string transferId, bool? onchain = null)
        {
            await _storage.ReplaceAsync(OffchainTransferEntity.ByCommon.GeneratePartitionKey(), transferId,
                entity =>
                {
                    entity.Completed = true;
                    if (onchain != null)
                        entity.Onchain = onchain.Value;
                    return entity;
                });
        }

        public async Task UpdateTransfer(string transferId, string externalTransferId, bool closing = false, bool? onchain = null)
        {
            await _storage.ReplaceAsync(OffchainTransferEntity.ByCommon.GeneratePartitionKey(), transferId,
                 entity =>
                 {
                     entity.ExternalTransferId = externalTransferId;
                     entity.ChannelClosing = closing;
                     if (onchain != null)
                         entity.Onchain = onchain.Value;
                     return entity;
                 });
        }

        public async Task<IEnumerable<IOffchainTransfer>> GetTransfersByDate(OffchainTransferType type, DateTimeOffset @from, DateTimeOffset to)
        {
            var filter1 = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                    OffchainTransferEntity.ByCommon.GeneratePartitionKey()),
                TableOperators.And,
                TableQuery.GenerateFilterConditionForInt("Type", QueryComparisons.Equal, (int)type)
            );

            var filter2 = TableQuery.CombineFilters(
                TableQuery.GenerateFilterConditionForDate("CreatedDt", QueryComparisons.GreaterThanOrEqual, from),
                TableOperators.And,
                TableQuery.GenerateFilterConditionForDate("CreatedDt", QueryComparisons.LessThanOrEqual, to)
            );

            var query = new TableQuery<OffchainTransferEntity>().Where(
                TableQuery.CombineFilters(filter1, TableOperators.And, filter2));

            return await _storage.WhereAsync(query);
        }

        public async Task<IEnumerable<IOffchainTransfer>> GetTransfers(DateTime from, DateTime to)
        {
            var date1 = TableQuery.GenerateFilterConditionForDate(
                "CreatedDt", QueryComparisons.GreaterThanOrEqual,
                new DateTimeOffset(from));
            var date2 = TableQuery.GenerateFilterConditionForDate(
                "CreatedDt", QueryComparisons.LessThanOrEqual,
                new DateTimeOffset(to));

            string finalFilter = TableQuery.CombineFilters(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, OffchainTransferEntity.ByCommon.GeneratePartitionKey()),
                    TableOperators.And,
                    date1),
                TableOperators.And, date2);

            return await _storage.WhereAsync(new TableQuery<OffchainTransferEntity>().Where(finalFilter));
        }

        public async Task AddChildTransfer(string transferId, IOffchainTransfer child)
        {
            await _storage.MergeAsync(OffchainTransferEntity.ByCommon.GeneratePartitionKey(), transferId,
                entity =>
                {
                    var data = entity.GetAdditionalData();
                    data.ChildTransfers.Add(child.Id);
                    entity.SetAdditionalData(data);

                    entity.Amount += child.Amount;

                    return entity;
                });
        }

        public async Task SetTransferIsChild(string transferId, string parentId)
        {
            await _storage.MergeAsync(OffchainTransferEntity.ByCommon.GeneratePartitionKey(), transferId,
                entity =>
                {
                    entity.IsChild = true;
                    entity.ParentTransferId = parentId;

                    return entity;
                });
        }
    }
}

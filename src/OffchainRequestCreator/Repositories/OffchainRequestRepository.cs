using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;

namespace OffchainRequestCreator.Repositories
{
    public class OffchainRequestEntity : BaseEntity, IOffchainRequest
    {
        public string RequestId => RowKey;
        public string TransferId { get; set; }

        public string AssetId { get; set; }

        public string ClientId { get; set; }

        public RequestType Type { get; set; }

        public DateTime? StartProcessing { get; set; }

        public DateTime CreateDt { get; set; }

        public int TryCount { get; set; }

        public OffchainTransferType TransferType { get; set; }

        public DateTime? ServerLock { get; set; }

        public static class ByRecord
        {
            public static string Partition = "OffchainSignatureRequestEntity";

            public static OffchainRequestEntity Create(string id, string transferId, string clientId, string assetId, RequestType type, OffchainTransferType transferType)
            {
                return new OffchainRequestEntity
                {
                    RowKey = id,
                    PartitionKey = Partition,
                    TransferId = transferId,
                    ClientId = clientId,
                    AssetId = assetId,
                    Type = type,
                    CreateDt = DateTime.UtcNow,
                    TransferType = transferType
                };
            }
        }

        public static class ByClient
        {
            public static string GeneratePartition(string clientId)
            {
                return clientId;
            }

            public static OffchainRequestEntity Create(string id, string transferId, string clientId, string assetId, RequestType type, OffchainTransferType transferType)
            {
                return new OffchainRequestEntity
                {
                    RowKey = id,
                    PartitionKey = GeneratePartition(clientId),
                    TransferId = transferId,
                    ClientId = clientId,
                    AssetId = assetId,
                    Type = type,
                    CreateDt = DateTime.UtcNow,
                    TransferType = transferType
                };
            }
        }

        public static class Archieved
        {
            public static string GeneratePartition()
            {
                return "Archieved";
            }

            public static OffchainRequestEntity Create(IOffchainRequest request)
            {
                return new OffchainRequestEntity
                {
                    RowKey = request.RequestId,
                    PartitionKey = GeneratePartition(),
                    TransferId = request.TransferId,
                    ClientId = request.ClientId,
                    AssetId = request.AssetId,
                    Type = request.Type,
                    CreateDt = request.CreateDt == DateTime.MinValue ? DateTime.UtcNow : request.CreateDt,
                    TryCount = request.TryCount,
                    TransferType = request.TransferType
                };
            }
        }
    }


    public class OffchainRequestRepository : IOffchainRequestRepository
    {
        private const int LockTimeoutSeconds = 100;

        private readonly INoSQLTableStorage<OffchainRequestEntity> _table;

        public OffchainRequestRepository(INoSQLTableStorage<OffchainRequestEntity> table)
        {
            _table = table;
        }

        public async Task<IOffchainRequest> CreateRequest(string transferId, string clientId, string assetId, RequestType type, OffchainTransferType transferType)
        {
            var id = Guid.NewGuid().ToString();

            var byClient = OffchainRequestEntity.ByClient.Create(id, transferId, clientId, assetId, type, transferType);
            await _table.InsertAsync(byClient);

            var byRecord = OffchainRequestEntity.ByRecord.Create(id, transferId, clientId, assetId, type, transferType);
            await _table.InsertAsync(byRecord);

            return byRecord;
        }

        public async Task<IEnumerable<IOffchainRequest>> GetRequestsForClient(string clientId)
        {
            return await _table.GetDataAsync(OffchainRequestEntity.ByClient.GeneratePartition(clientId));
        }

        public async Task<IEnumerable<IOffchainRequest>> GetCurrentRequests()
        {
            return await _table.GetDataAsync(OffchainRequestEntity.ByRecord.Partition);
        }

        public async Task<IOffchainRequest> GetRequest(string requestId)
        {
            return await _table.GetDataAsync(OffchainRequestEntity.ByRecord.Partition, requestId);
        }

        public async Task<IOffchainRequest> LockRequest(string requestId)
        {
            return await _table.ReplaceAsync(OffchainRequestEntity.ByRecord.Partition, requestId, entity =>
            {
                if (entity.StartProcessing == null || (DateTime.UtcNow - entity.StartProcessing.Value).TotalSeconds > LockTimeoutSeconds)
                {
                    entity.StartProcessing = DateTime.UtcNow;
                    entity.TryCount++;

                    //TODO: remove
                    if (entity.CreateDt == DateTime.MinValue)
                        entity.CreateDt = DateTime.UtcNow;

                    return entity;
                }
                return null;
            });
        }

        public async Task Complete(string requestId)
        {
            var record = await _table.DeleteAsync(OffchainRequestEntity.ByRecord.Partition, requestId);

            await _table.DeleteAsync(OffchainRequestEntity.ByClient.GeneratePartition(record.ClientId), requestId);

            await _table.InsertOrReplaceAsync(OffchainRequestEntity.Archieved.Create(record));
        }

        public async Task DeleteRequest(string requestId)
        {
            var record = await _table.DeleteAsync(OffchainRequestEntity.ByRecord.Partition, requestId);

            await _table.DeleteAsync(OffchainRequestEntity.ByClient.GeneratePartition(record.ClientId), requestId);
        }
    }
}

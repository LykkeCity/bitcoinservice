using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OffchainRequestCreator.Repositories
{
    public enum RequestType
    {
        None = 0,
        RequestTransfer = 1
    }

    public interface IOffchainRequest
    {
        string RequestId { get; }
        string TransferId { get; }

        string AssetId { get; }
        string ClientId { get; }

        RequestType Type { get; }

        DateTime? StartProcessing { get; }

        DateTime CreateDt { get; }

        int TryCount { get; }

        OffchainTransferType TransferType { get; }
    }

    public interface IOffchainRequestRepository
    {
        Task<IOffchainRequest> CreateRequest(string transferId, string clientId, string assetId, RequestType type, OffchainTransferType transferType);

        Task<IEnumerable<IOffchainRequest>> GetRequestsForClient(string clientId);

        Task<IEnumerable<IOffchainRequest>> GetCurrentRequests();

        Task<IOffchainRequest> GetRequest(string requestId);

        Task<IOffchainRequest> LockRequest(string requestId);

        Task Complete(string requestId);
    }
}

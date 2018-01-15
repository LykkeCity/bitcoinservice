using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RestEase;

namespace Core.Providers
{

    public class LykkeTransactionNotification
    {
        public LykkeTransactionNotification(Guid transactionId, string transactionHash)
        {
            TransactionId = transactionId;
            TransactionHash = transactionHash;
        }

        public Guid TransactionId { get; set; }
        public string TransactionHash { get; set; }
    }

    public class LykkeTransactionMultiNotification
    {
        public List<Guid> TransactionIds { get; }
        public string TransactionHash { get; }

        public LykkeTransactionMultiNotification(List<Guid> transactionIds, string transactionHash)
        {
            TransactionIds = transactionIds;
            TransactionHash = transactionHash;
        }
    }

    public interface ILykkeApiProvider
    {
        [Post("/api/PreBroadcastNotification")]
        Task SendPreBroadcastNotification([Body]LykkeTransactionNotification notification);

        [Post("/api/PostBroadcastNotification")]
        Task SendPostBroadcastNotification([Body]LykkeTransactionNotification notification);

        [Post("/api/PreBroadcastNotification/aggregatedCashout")]
        Task SendPreBroadcastMultiNotification([Body] LykkeTransactionMultiNotification nofification);

        [Post("/api/PostBroadcastNotification/aggregatedCashout")]
        Task SendPostBroadcastMultiNotification([Body] LykkeTransactionMultiNotification nofification);
    }
}

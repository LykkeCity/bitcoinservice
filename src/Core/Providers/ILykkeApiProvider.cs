using System;
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

    public interface ILykkeApiProvider
    {
        [Post("/api/PreBroadcastNotification")]
        Task SendPreBroadcastNotification([Body]LykkeTransactionNotification notification);

        [Post("/api/PostBroadcastNotification")]
        Task SendPostBroadcastNotification([Body]LykkeTransactionNotification notification);
    }
}

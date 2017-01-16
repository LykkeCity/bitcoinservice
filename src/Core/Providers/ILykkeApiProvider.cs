using System;
using System.Threading.Tasks;

namespace Core.Providers
{
    public interface ILykkeApiProvider
    {
        Task SendPreBroadcastNotification(Guid transactionId, string transactionHash);
        Task SendPostBroadcastNotification(Guid transactionId, string transactionHash);
    }
}

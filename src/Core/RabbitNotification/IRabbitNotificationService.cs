using System;

namespace Core.RabbitNotification
{
    public interface IRabbitNotificationService
    {
        void OpenChannel(string channelId, string transactionId, string assetId, string hubAddress, string clientAddress1, string clientAddress2);
        void CloseChannel(string channelId, string closeTransactionId);
        void Transfer(string channelId, string transactionId, decimal clientAddress1Quantity, decimal clientAddress2Quantity, DateTime timeAdd);
        void CreateMultisig(string multisig, DateTime timeAdd);
    }
}

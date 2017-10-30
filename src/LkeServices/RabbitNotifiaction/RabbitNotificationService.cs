using System;
using Common;
using Core;
using Core.RabbitNotification;

namespace LkeServices.RabbitNotifiaction
{
    public class RabbitNotificationService : IRabbitNotificationService
    {
        private readonly Func<string, IRabbitMqPublisher> _rabbitMqPublisherFactory;

        public RabbitNotificationService(Func<string, IRabbitMqPublisher> rabbitMqPublisherFactory)
        {
            _rabbitMqPublisherFactory = rabbitMqPublisherFactory;
        }


        public void OpenChannel(string channelId, string prevChannelId, string transactionId, string assetId, string hubAddress, string clientAddress1, string clientAddress2)
        {
            var publisher = _rabbitMqPublisherFactory(Constants.RabbitMqExplorerNotification);
            var data = BaseNotificationContract.Create(ChannelOpenedNotificationContract.OperationKey, ChannelOpenedNotificationContract.Create(channelId, prevChannelId, transactionId, assetId, hubAddress, clientAddress1, clientAddress2));            
            publisher.Publish(data.ToJson());
        }

        public void CloseChannel(string channelId, string closeTransactionId)
        {
            var publisher = _rabbitMqPublisherFactory(Constants.RabbitMqExplorerNotification);
            var data = BaseNotificationContract.Create(ChannelClosedNotificationContract.OperationKey,
                ChannelClosedNotificationContract.Create(channelId, closeTransactionId));
            publisher.Publish(data.ToJson());
        }

        public void Transfer(string channelId, string transactionId, decimal clientAddress1Quantity, decimal clientAddress2Quantity, DateTime timeAdd)
        {
            var publisher = _rabbitMqPublisherFactory(Constants.RabbitMqExplorerNotification);
            var data = BaseNotificationContract.Create(OffChainTransactionNotificationContract.OperationKey,
                OffChainTransactionNotificationContract.Create(
                    channelId, transactionId, clientAddress1Quantity, clientAddress2Quantity, timeAdd));
            publisher.Publish(data.ToJson());
        }

        public void CreateMultisig(string multisig, DateTime timeAdd)
        {
            var publisher = _rabbitMqPublisherFactory(Constants.RabbitMqMultisigNotification);
            var data = new AddressNotification
            {
                Address = multisig,
                Date = timeAdd
            };
            publisher.Publish(data.ToJson());
        }
    }
}

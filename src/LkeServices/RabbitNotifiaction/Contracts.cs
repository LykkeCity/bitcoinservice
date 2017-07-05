using System;
using Common;

namespace LkeServices.RabbitNotifiaction
{
    public class BaseNotificationContract
    {
        public string OperationType { get; set; }

        public string Data { get; set; }

        public static BaseNotificationContract Create(string operationType, object data)
        {
            return new BaseNotificationContract
            {
                OperationType = operationType,
                Data = data.ToJson()
            };
        }

        public T Convert<T>()
        {
            return Data.DeserializeJson<T>();
        }
    }

    public class ChannelOpenedNotificationContract
    {
        public const string OperationKey = "ChannelOpened";

        public string ChannelId { get; set; }

        public string OpenTransactionId { get; set; }
        public string AssetId { get; set; }
        public string HubAddress { get; set; }
        public string ClientAddress1 { get; set; }
        public string ClientAddress2 { get; set; }

        public static ChannelOpenedNotificationContract Create(string channelId, string transactionId, string assetId, string hubAddress, string clientAddress1, string clientAddress2)
        {
            return new ChannelOpenedNotificationContract
            {
                ChannelId = channelId,
                OpenTransactionId = transactionId,
                AssetId = assetId,
                ClientAddress1 = clientAddress1,
                ClientAddress2 = clientAddress2,
                HubAddress = hubAddress
            };
        }
    }

    public class ChannelClosedNotificationContract
    {
        public const string OperationKey = "ChannelClosed";

        public string ChannelId { get; set; }
        public string CloseTransactionId { get; set; }

        public static ChannelClosedNotificationContract Create(string channelId, string closeTransactionId)
        {
            return new ChannelClosedNotificationContract
            {
                ChannelId = channelId,
                CloseTransactionId = closeTransactionId
            };
        }
    }

    public class OffChainTransactionNotificationContract
    {
        public const string OperationKey = "OffchainTransactionAdded";

        public string ChannelId { get; set; }
        public string TransactionId { get; set; }
        public decimal ClientAddress1Quantity { get; set; }
        public decimal ClientAddress2Quantity { get; set; }

        public DateTime TimeAdd { get; set; }
        public static OffChainTransactionNotificationContract Create(string channelId,
            string transactionId,
            decimal clientAddress1Quantity,
            decimal clientAddress2Quantity,
            DateTime timeAdd)
        {
            return new OffChainTransactionNotificationContract
            {
                ChannelId = channelId,
                TransactionId = transactionId,
                ClientAddress1Quantity = clientAddress1Quantity,
                ClientAddress2Quantity = clientAddress2Quantity,
                TimeAdd = timeAdd
            };
        }
    }


    public class AddressNotification
    {
        public string Address { get; set; }

        public DateTime Date { get; set; }
    }
}
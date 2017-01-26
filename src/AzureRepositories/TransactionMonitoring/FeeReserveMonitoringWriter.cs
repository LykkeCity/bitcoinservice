using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Core;
using Core.Bitcoin;
using Core.TransactionMonitoring;
using NBitcoin;
using Common;

namespace AzureRepositories.TransactionMonitoring
{
    public class FeeReserveMonitoringWriter : IFeeReserveMonitoringWriter
    {
        private readonly IQueueExt _queue;

        public FeeReserveMonitoringWriter(Func<string, IQueueExt> queueFactory)
        {
            _queue = queueFactory(Constants.FeeReserveMonitoringQueue);
        }

        public Task AddTransactionFeeReserve(Guid transactionId, List<ICoin> feeCoins)
        {
            var message = new FeeReserveMonitoringMessage
            {
                PutDateTime = DateTime.UtcNow,
                TransactionId = transactionId,
                FeeCoins = feeCoins.OfType<Coin>().Select(x => new SerializableCoin(x)).ToList()
            };

            return _queue.PutRawMessageAsync(message.ToJson());
        }
    }
}

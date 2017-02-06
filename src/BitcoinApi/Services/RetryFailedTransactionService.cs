using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Common.Log;
using Core;
using Core.Exceptions;
using Core.TransactionMonitoring;
using Core.TransactionQueueWriter;

namespace BitcoinApi.Services
{
    public interface IRetryFailedTransactionService
    {
        Task RetryTransaction(Guid transactionId);
    }

    public class RetryFailedTransactionService : IRetryFailedTransactionService
    {
        private const string PoisonSuffix = "-poison";
        private readonly Func<string, IQueueExt> _queueFactory;
        private readonly ILog _logger;

        public RetryFailedTransactionService(Func<string, IQueueExt> queueFactory, ILog logger)
        {
            _queueFactory = queueFactory;
            _logger = logger;
        }

        public async Task RetryTransaction(Guid transactionId)
        {
            if (await RetryCommandsQueue(transactionId))
                return;
            if (await RetryBroadcastQueue(transactionId))
                return;
            if (await RetryBroadcastMonitoringQueue(transactionId))
                return;

            throw new BackendException("Transaction not found", ErrorCode.BadInputParameter);
        }

        private async Task<bool> RetryCommandsQueue(Guid transactionId)
        {
            var poisonQueue = _queueFactory(Constants.TransactionCommandQueue + PoisonSuffix);
            var count = await poisonQueue.Count() ?? 0;
            for (int i = 0; i < count; i++)
            {
                var msg = await poisonQueue.GetRawMessageAsync();
                var obj = msg.AsString.DeserializeJson<TransactionQueueMessage>();
                if (obj.TransactionId == transactionId)
                {
                    var queue = _queueFactory(Constants.TransactionCommandQueue);

                    obj.DequeueCount = 0;
                    obj.LastError = "";

                    await queue.PutRawMessageAsync(obj.ToJson());
                    await poisonQueue.FinishRawMessageAsync(msg);

                    await _logger.WriteInfoAsync("RetryFailedTransactionService", "RetryCommandsQueue", transactionId.ToString(), "Transaction was retried");
                    return true;
                }
                await poisonQueue.PutRawMessageAsync(msg.AsString);
                await poisonQueue.FinishRawMessageAsync(msg);
            }

            return false;
        }

        private async Task<bool> RetryBroadcastQueue(Guid transactionId)
        {
            var poisonQueue = _queueFactory(Constants.BroadcastingQueue + PoisonSuffix);
            var count = await poisonQueue.Count() ?? 0;
            for (int i = 0; i < count; i++)
            {
                var msg = await poisonQueue.GetRawMessageAsync();
                var obj = msg.AsString.DeserializeJson<BroadcastingTransaction>();
                if (obj.TransactionId == transactionId)
                {
                    var queue = _queueFactory(Constants.BroadcastingQueue);

                    obj.DequeueCount = 0;
                    obj.LastError = "";

                    await queue.PutRawMessageAsync(obj.ToJson());
                    await poisonQueue.FinishRawMessageAsync(msg);

                    await _logger.WriteInfoAsync("RetryFailedTransactionService", "RetryBroadcastQueue", transactionId.ToString(), "Transaction was retried");
                    return true;
                }
                await poisonQueue.PutRawMessageAsync(msg.AsString);
                await poisonQueue.FinishRawMessageAsync(msg);
            }

            return false;
        }

        private async Task<bool> RetryBroadcastMonitoringQueue(Guid transactionId)
        {
            var poisonQueue = _queueFactory(Constants.BroadcastMonitoringQueue + PoisonSuffix);
            var count = await poisonQueue.Count() ?? 0;
            for (int i = 0; i < count; i++)
            {
                var msg = await poisonQueue.GetRawMessageAsync();
                var obj = msg.AsString.DeserializeJson<TransactionMonitoringMessage>();
                if (obj.TransactionId == transactionId)
                {
                    var queue = _queueFactory(Constants.BroadcastMonitoringQueue);

                    obj.PutDateTime = DateTime.UtcNow;
                    obj.LastError = "";

                    await queue.PutRawMessageAsync(obj.ToJson());
                    await poisonQueue.FinishRawMessageAsync(msg);

                    await _logger.WriteInfoAsync("RetryFailedTransactionService", "RetryBroadcastMonitoringQueue", transactionId.ToString(), "Transaction was retried");
                    return true;
                }
                await poisonQueue.PutRawMessageAsync(msg.AsString);
                await poisonQueue.FinishRawMessageAsync(msg);
            }

            return false;
        }
    }
}

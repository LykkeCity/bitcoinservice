using System;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Common.Log;
using Core;
using Core.Bitcoin;
using Core.Settings;
using Core.TransactionMonitoring;
using LkeServices.Triggers.Attributes;
using LkeServices.Triggers.Bindings;
using NBitcoin;
using NBitcoin.RPC;

namespace BackgroundWorker.Functions
{
    public class BroadcastingTransactionFunction
    {
        private readonly IBitcoinBroadcastService _broadcastService;
        private readonly IFailedTransactionRepository _failedTransactionRepository;
        private readonly BaseSettings _settings;
        private readonly ILog _logger;

        public BroadcastingTransactionFunction(IBitcoinBroadcastService broadcastService, 
            IFailedTransactionRepository failedTransactionRepository,
            BaseSettings settings, ILog logger)
        {
            _broadcastService = broadcastService;
            _failedTransactionRepository = failedTransactionRepository;
            _settings = settings;
            _logger = logger;
        }

        [QueueTrigger(Constants.BroadcastingQueue, 100, true)]
        public async Task BroadcastTransaction(BroadcastingTransaction transaction, QueueTriggeringContext context)
        {
            try
            {
                var tr = new Transaction(transaction.TransactionHex);
                await _broadcastService.BroadcastTransaction(transaction.TransactionId, tr);
            }
            catch (RPCException e)
            {
                if (e.RPCCodeMessage != transaction.LastError)
                    await _logger.WriteWarningAsync("BroadcastingTransactionFunction", "BroadcastTransaction", $"Id: [{transaction.TransactionId}]", $"Message: {e.RPCCodeMessage} Code:{e.RPCCode}");

                transaction.LastError = e.RPCCodeMessage;

                if (transaction.DequeueCount >= _settings.MaxDequeueCount)
                {
                    context.MoveMessageToPoison();
                    await _failedTransactionRepository.AddFailedTransaction(transaction.TransactionId, null);
                }
                else
                {
                    transaction.DequeueCount++;
                    context.MoveMessageToEnd(transaction.ToJson());
                    context.SetCountQueueBasedDelay(_settings.MaxQueueDelay, 200);
                }
            }
        }
    }
}

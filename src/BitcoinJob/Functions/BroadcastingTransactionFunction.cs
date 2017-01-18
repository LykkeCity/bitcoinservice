using System.Threading.Tasks;
using Common;
using Common.Log;
using Core;
using Core.Bitcoin;
using Core.Settings;
using LkeServices.Triggers.Attributes;
using LkeServices.Triggers.Bindings;
using NBitcoin;
using NBitcoin.RPC;

namespace BackgroundWorker.Functions
{
    public class BroadcastingTransactionFunction
    {
        private readonly IBitcoinBroadcastService _broadcastService;
        private readonly BaseSettings _settings;
        private readonly ILog _logger;

        public BroadcastingTransactionFunction(IBitcoinBroadcastService broadcastService, BaseSettings settings, ILog logger)
        {
            _broadcastService = broadcastService;
            _settings = settings;
            _logger = logger;
        }

        [QueueTrigger(Constants.BroadcastingQueue, 100, true)]
        public async Task BroadcastTransaction(BroadcastingTransaction transaction, QueueTriggeringContext context)
        {
            try
            {
                await _broadcastService.BroadcastTransaction(transaction.TransactionId, new Transaction(transaction.TransactionHex));
            }
            catch (RPCException e)
            {
                await _logger.WriteErrorAsync("BroadcastingTransactionFunction", "BroadcastTransaction", $"Id: [{transaction.TransactionId}]", e);
                if (transaction.DequeueCount >= _settings.MaxDequeueCount)
                    context.MoveMessageToPoison();
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

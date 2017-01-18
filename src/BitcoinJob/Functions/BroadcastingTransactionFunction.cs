using System.Threading.Tasks;
using Common;
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

        public BroadcastingTransactionFunction(IBitcoinBroadcastService broadcastService, BaseSettings settings)
        {
            _broadcastService = broadcastService;
            _settings = settings;
        }

        [QueueTrigger(Constants.BroadcastingQueue, 100)]
        public async Task BroadcastTransaction(BroadcastingTransaction transaction, QueueTriggeringContext context)
        {
            try
            {
                await _broadcastService.BroadcastTransaction(transaction.TransactionId, new Transaction(transaction.TransactionHex));
            }
            catch (RPCException)
            {
                if (transaction.DequeueCount >= _settings.MaxDequeueCount)
                    context.MoveMessageToPoision();
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

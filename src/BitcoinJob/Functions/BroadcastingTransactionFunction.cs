using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core;
using Core.Bitcoin;
using Core.Providers;
using Core.Repositories.Transactions;
using Core.Settings;
using Core.TransactionQueueWriter;
using LkeServices.Providers;
using LkeServices.Transactions;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;
using NBitcoin;
using NBitcoin.RPC;

namespace BitcoinJob.Functions
{
    public class BroadcastingTransactionFunction
    {
        private readonly IBitcoinBroadcastService _broadcastService;
        private readonly ITransactionBlobStorage _transactionBlobStorage;
        private readonly BaseSettings _settings;
        private readonly ILog _logger;        
        private readonly ISignatureApiProvider _exchangeSignatureApi;

        public BroadcastingTransactionFunction(IBitcoinBroadcastService broadcastService,
            ITransactionBlobStorage transactionBlobStorage,
            Func<SignatureApiProviderType, ISignatureApiProvider> signatureApiProviderFactory,
            BaseSettings settings, ILog logger)
        {
            _broadcastService = broadcastService;
            _transactionBlobStorage = transactionBlobStorage;
            _settings = settings;
            _logger = logger;
            
            _exchangeSignatureApi = signatureApiProviderFactory(SignatureApiProviderType.Exchange);
        }

        [QueueTrigger(Constants.BroadcastingQueue, 100, true)]
        public async Task BroadcastTransaction(BroadcastingTransaction transaction, QueueTriggeringContext context)
        {
            try
            {
                var signedByClientTr = await _transactionBlobStorage.GetTransaction(transaction.TransactionId, TransactionBlobType.Client);
                               
                var signedByExchangeTr = await _exchangeSignatureApi.SignTransaction(signedByClientTr);

                var tr = new Transaction(signedByExchangeTr);
                await _broadcastService.BroadcastTransaction(transaction.TransactionId, tr);
            }
            catch (RPCException e)
            {
                if (e.Message != transaction.LastError)
                    await _logger.WriteWarningAsync("BroadcastingTransactionFunction", "BroadcastTransaction", $"Id: [{transaction.TransactionId}]", $"Message: {e.Message} Code:{e.RPCCode} CodeMessage:{e.RPCCodeMessage}");

                transaction.LastError = e.Message;

                if (transaction.DequeueCount >= _settings.MaxDequeueCount)
                {
                    context.MoveMessageToPoison();
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

using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core;
using Core.Bitcoin;
using Core.Providers;
using Core.Repositories.Settings;
using Core.Repositories.Transactions;
using Core.Settings;
using Core.TransactionQueueWriter;
using LkeServices.Providers;
using LkeServices.Transactions;
using Lykke.Bitcoin.Contracts;
using Lykke.Bitcoin.Contracts.Events;
using Lykke.Cqrs;
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
        private readonly ISettingsRepository _settingsRepository;
        private readonly ICqrsEngine _cqrsEngine;

        private readonly string[] _unacceptableTxErrors = { "bad-txns-inputs-spent", "txn-mempool-conflict", "absurdly-high-fee" };

        public BroadcastingTransactionFunction(IBitcoinBroadcastService broadcastService,
            ITransactionBlobStorage transactionBlobStorage,
            ISignatureApiProvider signatureApiProvider,
            BaseSettings settings, ILog logger, ISettingsRepository settingsRepository, ICqrsEngine cqrsEngine)
        {
            _broadcastService = broadcastService;
            _transactionBlobStorage = transactionBlobStorage;
            _settings = settings;
            _logger = logger;
            _settingsRepository = settingsRepository;
            _cqrsEngine = cqrsEngine;

            _exchangeSignatureApi = signatureApiProvider;
        }

        [QueueTrigger(Constants.BroadcastingQueue, 100)]
        public async Task BroadcastTransaction(BroadcastingTransaction transaction, QueueTriggeringContext context)
        {
            try
            {
                var signedByClientTr = await _transactionBlobStorage.GetTransaction(transaction.TransactionId, TransactionBlobType.Initial);

                var signedByExchangeTr = await _exchangeSignatureApi.SignTransaction(signedByClientTr);

                if (!await _settingsRepository.Get(Constants.CanBeBroadcastedSetting, true))
                {
                    await _transactionBlobStorage.AddOrReplaceTransaction(transaction.TransactionId, TransactionBlobType.Signed, signedByExchangeTr);
                    context.MoveMessageToPoison(transaction.ToJson());
                    return;
                }

                var tr = new Transaction(signedByExchangeTr);

                await _transactionBlobStorage.AddOrReplaceTransaction(transaction.TransactionId, TransactionBlobType.Prebroadcasted, signedByExchangeTr);

                await _broadcastService.BroadcastTransaction(transaction.TransactionId, tr);

                if (transaction.TransactionCommandType == TransactionCommandType.SegwitTransferToHotwallet)
                {
                    _cqrsEngine.PublishEvent(new CashinCompletedEvent { OperationId = transaction.TransactionId, TxHash = tr.GetHash().ToString() }, BitcoinBoundedContext.Name);
                }

                if (transaction.TransactionCommandType == TransactionCommandType.Transfer)
                {
                    _cqrsEngine.PublishEvent(new CashoutCompletedEvent { OperationId = transaction.TransactionId, TxHash = tr.GetHash().ToString() }, BitcoinBoundedContext.Name);
                }

            }
            catch (RPCException e)
            {

                if (e.Message != transaction.LastError)
                    await _logger.WriteWarningAsync("BroadcastingTransactionFunction", "BroadcastTransaction", $"Id: [{transaction.TransactionId}]", $"Message: {e.Message} Code:{e.RPCCode} CodeMessage:{e.RPCCodeMessage}");

                transaction.LastError = e.Message;

                var unacceptableTx = _unacceptableTxErrors.Any(o => e.Message.Contains(o));

                if (transaction.DequeueCount >= _settings.MaxDequeueCount || unacceptableTx)
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

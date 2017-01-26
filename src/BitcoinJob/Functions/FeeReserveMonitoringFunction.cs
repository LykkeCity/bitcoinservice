using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Transactions;
using Core.Repositories.TransactionSign;
using Core.Settings;
using Core.TransactionMonitoring;
using LkeServices.Triggers.Attributes;
using LkeServices.Triggers.Bindings;
using NBitcoin;

namespace BackgroundWorker.Functions
{
    public class FeeReserveMonitoringFunction
    {
        private readonly BaseSettings _settings;
        private readonly IBroadcastedTransactionRepository _broadcastedTransactionRepository;
        private readonly ITransactionSignRequestRepository _transactionSignRequestRepository;
        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;
        private readonly ITransactionBlobStorage _transactionBlobStorage;
        private readonly ISpentOutputRepository _spentOutputRepository;

        public FeeReserveMonitoringFunction(BaseSettings settings, IBroadcastedTransactionRepository broadcastedTransactionRepository, ITransactionSignRequestRepository transactionSignRequestRepository, IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory, ITransactionBlobStorage transactionBlobStorage, ISpentOutputRepository spentOutputRepository)
        {
            _settings = settings;
            _broadcastedTransactionRepository = broadcastedTransactionRepository;
            _transactionSignRequestRepository = transactionSignRequestRepository;
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
            _transactionBlobStorage = transactionBlobStorage;
            _spentOutputRepository = spentOutputRepository;
        }

        [QueueTrigger(Constants.FeeReserveMonitoringQueue)]
        public async Task Monitor(FeeReserveMonitoringMessage message, QueueTriggeringContext context)
        {
            if (await _broadcastedTransactionRepository.IsBroadcasted(message.TransactionId))
                return;

            if (DateTime.UtcNow - message.PutDateTime > TimeSpan.FromSeconds(_settings.FeeReservePeriodSeconds))
            {
                await _transactionSignRequestRepository.InvalidateTransactionId(message.TransactionId);

                await Task.Delay(3000);

                if (await _broadcastedTransactionRepository.IsBroadcasted(message.TransactionId))
                    return;

                var transaction = await _transactionBlobStorage.GetTransaction(message.TransactionId, TransactionBlobType.Initial);

                var tr = new Transaction(transaction);

                await _spentOutputRepository.RemoveSpentOutputs(tr.Inputs.Select(x => new Output(x.PrevOut)));

                var queue = _pregeneratedOutputsQueueFactory.CreateFeeQueue();

                await queue.EnqueueOutputs(message.FeeCoins.Select(x => x.ToCoin()).ToArray());
            }
            else
            {
                context.MoveMessageToEnd();
                context.SetCountQueueBasedDelay(5000, 100);
            }
        }
    }
}

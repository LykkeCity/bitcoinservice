using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core;
using Core.Outputs;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Transactions;
using Core.Repositories.TransactionSign;
using Core.Settings;
using Core.TransactionMonitoring;
using LkeServices.Transactions;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;
using NBitcoin;

namespace BitcoinJob.Functions
{
    public class FeeReserveMonitoringFunction
    {
        private readonly BaseSettings _settings;
        private readonly ISpentOutputService _spentOutputService;
        private readonly ITransactionSignRequestRepository _transactionSignRequestRepository;
        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;
        private readonly ITransactionBlobStorage _transactionBlobStorage;        
        private readonly ILog _logger;
        private IBroadcastedTransactionBlobStorage _broadcastedTransactionBlob;

        public FeeReserveMonitoringFunction(BaseSettings settings, ISpentOutputService spentOutputService, IBroadcastedTransactionBlobStorage broadcastedTransactionBlob, ITransactionSignRequestRepository transactionSignRequestRepository, IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory, ITransactionBlobStorage transactionBlobStorage, ISpentOutputRepository spentOutputRepository, ILog logger)
        {
            _settings = settings;
            _spentOutputService = spentOutputService;
            _broadcastedTransactionBlob = broadcastedTransactionBlob;
            _transactionSignRequestRepository = transactionSignRequestRepository;
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
            _transactionBlobStorage = transactionBlobStorage;          
            _logger = logger;
        }

        [QueueTrigger(Constants.FeeReserveMonitoringQueue)]
        public async Task Monitor(FeeReserveMonitoringMessage message, QueueTriggeringContext context)
        {
            if (await _broadcastedTransactionBlob.IsBroadcasted(message.TransactionId))
                return;

            if (DateTime.UtcNow - message.PutDateTime > TimeSpan.FromSeconds(_settings.FeeReservePeriodSeconds))
            {
                await _logger.WriteInfoAsync("FeeReserveMonitoringFunction", "Monitor", message.ToJson(), "Free reserved fee");

                await _transactionSignRequestRepository.InvalidateTransactionId(message.TransactionId);

                await Task.Delay(3000);

                if (await _broadcastedTransactionBlob.IsBroadcasted(message.TransactionId))
                    return;

                var transaction = await _transactionBlobStorage.GetTransaction(message.TransactionId, TransactionBlobType.Initial);

                var tr = new Transaction(transaction);

                await _spentOutputService.RemoveSpentOutputs(tr);                

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

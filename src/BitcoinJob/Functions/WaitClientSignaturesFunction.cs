using System;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
using Core;
using Core.Providers;
using Core.Repositories.Transactions;
using Core.Settings;
using Core.TransactionQueueWriter;
using LkeServices.Providers;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;

namespace BitcoinJob.Functions
{
    public class WaitClientSignatureMessage
    {
        public Guid TransactionId { get; set; }

        public DateTime PutDateTime { get; set; }
    }

    public class WaitClientSignaturesFunction
    {
        private readonly Func<string, IQueueExt> _queueFactory;
        private readonly ITransactionBlobStorage _transactionBlobStorage;
        private readonly TimeSpan _timeout;
        private readonly ISignatureApiProvider _clientSignatureApi;

        public WaitClientSignaturesFunction(Func<string, IQueueExt> queueFactory,
            Func<SignatureApiProviderType, ISignatureApiProvider> signatureApiProviderFactory, ITransactionBlobStorage transactionBlobStorage, BaseSettings settings)
        {
            _queueFactory = queueFactory;
            _transactionBlobStorage = transactionBlobStorage;
            _timeout = TimeSpan.FromSeconds(settings.ClientSignatureTimeoutSeconds);
            _clientSignatureApi = signatureApiProviderFactory(SignatureApiProviderType.Client);
        }

        [QueueTrigger(Constants.ClientSignMonitoringQueue, 100, true)]
        public async Task ProcessMessage(WaitClientSignatureMessage message, QueueTriggeringContext context)
        {
            var signedByClient = await _transactionBlobStorage.GetTransaction(message.TransactionId, TransactionBlobType.Client);
            if (!string.IsNullOrEmpty(signedByClient))
            {
                await SendToBroadcast(message.TransactionId);
                return;
            }

            if (DateTime.UtcNow - message.PutDateTime > _timeout)
            {
                var initial = await _transactionBlobStorage.GetTransaction(message.TransactionId, TransactionBlobType.Initial);
                var signed = await _clientSignatureApi.SignTransaction(initial);
                await _transactionBlobStorage.AddOrReplaceTransaction(message.TransactionId, TransactionBlobType.Client, signed);
                await SendToBroadcast(message.TransactionId);
            }
            else
            {
                context.MoveMessageToEnd(message.ToJson());
                context.SetCountQueueBasedDelay(5000, 100);
            }
        }

        private Task SendToBroadcast(Guid transactionId)
        {
            return _queueFactory(Constants.BroadcastingQueue).PutRawMessageAsync(new BroadcastingTransaction
            {
                TransactionId = transactionId
            }.ToJson());
        }
    }
}

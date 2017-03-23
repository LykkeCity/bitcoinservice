using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Core;
using Core.Helpers;
using Core.Repositories.Transactions;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;

namespace BackgroundWorker.Functions
{
    public class ClientSignedTransactionMessage
    {
        public Guid TransactionId { get; set; }

        public string Transaction { get; set; }

        public string Error { get; set; }
    }

    public class ClientSignaturesFunction
    {
        private readonly ITransactionBlobStorage _transactionBlobStorage;

        public ClientSignaturesFunction(ITransactionBlobStorage transactionBlobStorage)
        {
            _transactionBlobStorage = transactionBlobStorage;
        }

        [QueueTrigger(Constants.ClientSignedTransactionQueue, 100, true, "client")]
        public async Task ProcessMessage(ClientSignedTransactionMessage message, QueueTriggeringContext context)
        {
            var initialTr = await _transactionBlobStorage.GetTransaction(message.TransactionId, TransactionBlobType.Initial);
            if (string.IsNullOrEmpty(initialTr))
            {
                message.Error = "Inital transaction was not found";
                context.MoveMessageToPoison(message.ToJson());
                return;
            }

            if (!TransactionComparer.CompareTransactions(initialTr, message.Transaction))
            {
                message.Error = "Client transaction is not equal to initial transaction";
                context.MoveMessageToPoison(message.ToJson());
                return;
            }

            await _transactionBlobStorage.AddOrReplaceTransaction(message.TransactionId, TransactionBlobType.Client, message.Transaction);
        }
    }
}

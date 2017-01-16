using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Bitcoin;
using Core.Exceptions;
using Core.Helpers;
using Core.OpenAssets;
using Core.Providers;
using Core.Repositories.TransactionSign;
using LkeServices.Triggers.Attributes;
using NBitcoin;

namespace BackgroundWorker.Functions
{
    public class SignedTransactionsFunction
    {
        private readonly ITransactionSignRequestRepository _transactionSignRequestRepository;
        private readonly ISignatureApiProvider _signatureApiProvider;
        private readonly IBitcoinBroadcastService _broadcastService;

        public SignedTransactionsFunction(ITransactionSignRequestRepository transactionSignRequestRepository,
            ISignatureApiProvider signatureApiProvider, IBitcoinBroadcastService broadcastService)
        {
            _transactionSignRequestRepository = transactionSignRequestRepository;
            _signatureApiProvider = signatureApiProvider;
            _broadcastService = broadcastService;
        }

        [QueueTrigger(Constants.SignedTransactionsQueue, 100)]
        public async Task IncomeMessage(SignedTransaction signedTransaction)
        {
            var signRequest = await _transactionSignRequestRepository.GetSignRequest(signedTransaction.TransactionId);

            if (!TransactionComparer.CompareTransactions(signRequest.InitialTransaction, signedTransaction.Transaction))
                throw new BackendException("Signed transaction is not equals to initial transaction", ErrorCode.BadTransaction);

            var result = await _transactionSignRequestRepository.SetSignedTransaction(signedTransaction.TransactionId, signedTransaction.Transaction);
            if (result.SignedTransaction1 != null && result.RequiredSignCount == 1 ||
                result.SignedTransaction1 != null && result.SignedTransaction2 != null && result.RequiredSignCount == 2)
            {
                var tr = result.SignedTransaction1;
                if (result.RequiredSignCount == 2)
                    tr = OpenAssetsHelper.MergeTransactionsSignatures(result.SignedTransaction1, result.SignedTransaction2);

                var fullSignedHex = await _signatureApiProvider.SignTransaction(tr);
                var fullSigned = new Transaction(fullSignedHex);

                await _broadcastService.BroadcastTransaction(signedTransaction.TransactionId, fullSigned);
            }
        }
    }

    public class SignedTransaction
    {
        public Guid TransactionId { get; set; }

        public string Transaction { get; set; }
    }
}

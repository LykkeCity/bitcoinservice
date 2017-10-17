using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core;
using Core.Bcc;
using Core.OpenAssets;
using LkeServices.Transactions;
using Lykke.JobTriggers.Triggers.Attributes;

namespace BitcoinJob.Functions
{
    public class BccTransferFunction
    {
        private readonly IBccTransactionService _bccTransactionService;

        public BccTransferFunction(IBccTransactionService bccTransactionService)
        {
            _bccTransactionService = bccTransactionService;
        }


        [QueueTrigger(Constants.BccTransferQueue, 100)]
        public Task OnMessage(BccTransferCommand command)
        {
            return _bccTransactionService.Transfer(OpenAssetsHelper.ParseAddress(command.SourceAddress),
                OpenAssetsHelper.ParseAddress(command.DestinationAddress), command.Amount);
        }
    }
}

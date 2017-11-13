using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common;
using Core;
using Core.QBitNinja;
using Core.Repositories.PaidFees;
using Lykke.JobTriggers.Triggers.Attributes;
using Lykke.JobTriggers.Triggers.Bindings;
using NBitcoin;

namespace BitcoinJob.Functions
{
    public class PaidFeesTaskFunction
    {
        private readonly IQBitNinjaApiCaller _qBitNinjaApi;
        private readonly IPaidFeesRepository _paidFeesRepository;

        public PaidFeesTaskFunction(IQBitNinjaApiCaller qBitNinjaApi, IPaidFeesRepository paidFeesRepository)
        {
            _qBitNinjaApi = qBitNinjaApi;
            _paidFeesRepository = paidFeesRepository;
        }

        [QueueTrigger(Constants.PaidFeesTasksQueue)]
        public async Task Process(PaidFeesTask task, QueueTriggeringContext context)
        {
            var tr = await _qBitNinjaApi.GetTransaction(task.TransactionHash);
            if (tr == null)
            {
                if (task.TryCount > PaidFeesTask.MaxTryCount)
                {
                    context.MoveMessageToPoison();
                    return;
                }
                task.TryCount++;
                context.MoveMessageToEnd(task.ToJson());
                return;
            }
            await _paidFeesRepository.Insert(task.TransactionHash, tr.Fees.ToDecimal(MoneyUnit.BTC), task.Date, task.Multisig, task.Asset);
        }
    }
}

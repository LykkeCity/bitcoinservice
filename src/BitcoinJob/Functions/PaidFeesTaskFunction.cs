using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core;
using Core.QBitNinja;
using Core.Repositories.PaidFees;
using Lykke.JobTriggers.Triggers.Attributes;
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
        public async Task Process(PaidFeesTask task)
        {
            var tr = await _qBitNinjaApi.GetTransaction(task.TransactionHash);
            await _paidFeesRepository.Insert(task.TransactionHash, tr.Fees.ToDecimal(MoneyUnit.BTC), task.Date, task.Multisig, task.Asset);
        }
    }
}

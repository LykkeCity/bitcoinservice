using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Queue;
using Common;
using Core;
using Core.Repositories.PaidFees;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Transactions;
using Core.Repositories.TransactionSign;
using Core.TransactionQueueWriter;
using Core.TransactionQueueWriter.Commands;
using Microsoft.WindowsAzure.Storage.Queue;
using NBitcoin;

namespace EnqueuePaidFeesTasks
{
    public class Job
    {
        private readonly IPaidFeesTaskWriter _paidFeesTaskWriter;
        private readonly IPaidFeesRepository _paidFeesRepository;
        private readonly IBroadcastedTransactionRepository _broadcastedTransactionRepository;


        public Job(IPaidFeesTaskWriter paidFeesTaskWriter, IPaidFeesRepository paidFeesRepository, IBroadcastedTransactionRepository broadcastedTransactionRepository)
        {
            _paidFeesTaskWriter = paidFeesTaskWriter;
            _paidFeesRepository = paidFeesRepository;
            _broadcastedTransactionRepository = broadcastedTransactionRepository;
        }

        public async Task Start()
        {
            var list = (await _broadcastedTransactionRepository.GetTrasactions(new DateTime(2017, 12, 13), new DateTime(2018, 1, 1))).ToList();
            foreach (var tx in list)
            {
                if(await _paidFeesRepository.Has(tx.Hash))
                    continue;
                await _paidFeesTaskWriter.AddTask(tx.Hash, tx.Date, null, null);
            }
        }

        public async Task Report()
        {
            var startDt = new DateTime(2017, 12, 1);
            var endDt = new DateTime(2018,1,1);

            IEnumerable<IPaidFees> fees = await _paidFeesRepository.Get(startDt, endDt);
            var builder = new StringBuilder();

            foreach (var fee in fees.OrderBy(o => o.Date))
            {
                builder.Append(fee.Date.ToString("dd.MM.yyyy")).Append(";");
                builder.Append(fee.TransactionHash).Append(";"); ;
                builder.Append(fee.Amount).Append(";").Append(Environment.NewLine);
            }

            File.WriteAllText("out.csv", builder.ToString());
        }

    }
}

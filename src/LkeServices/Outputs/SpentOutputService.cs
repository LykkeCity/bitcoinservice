using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Outputs;
using Core.Repositories.TransactionOutputs;
using NBitcoin;

namespace LkeServices.Outputs
{
    public class SpentOutputService  : ISpentOutputService
    {
        private readonly ISpentOutputRepository _spentOutputRepository;
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;

        public SpentOutputService(ISpentOutputRepository spentOutputRepository, IBroadcastedOutputRepository broadcastedOutputRepository)
        {
            _spentOutputRepository = spentOutputRepository;
            _broadcastedOutputRepository = broadcastedOutputRepository;
        }

        public async Task SaveSpentOutputs(Guid transactionId, Transaction transaction)
        {
            await _spentOutputRepository.InsertSpentOutputs(transactionId, transaction.Inputs.Select(o => new Output(o.PrevOut)));
            var tasks = new List<Task>();
            foreach (var outPoint in transaction.Inputs.Select(o => o.PrevOut))
                tasks.Add(_broadcastedOutputRepository.DeleteOutput(outPoint.Hash.ToString(), (int)outPoint.N));
            await Task.WhenAll(tasks);
        }

        public Task RemoveSpenOutputs(Transaction transaction)
        {
            return _spentOutputRepository.RemoveSpentOutputs(transaction.Inputs.Select(o => new Output(o.PrevOut)));
        }
    }
}

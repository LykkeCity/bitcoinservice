using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NBitcoin;

namespace Core.Repositories.TransactionOutputs
{
    public interface IOutput
    {
        string TransactionHash { get; set; }

        int N { get; set; }
    }

    public interface ISpentOutputRepository
    {
        Task InsertSpentOutputs(Guid transactionId, IEnumerable<IOutput> outputs);

        Task<IEnumerable<IOutput>> GetUnspentOutputs(IEnumerable<IOutput> outputs);

        Task RemoveSpentOutputs(IEnumerable<IOutput> outputs);
    }
}

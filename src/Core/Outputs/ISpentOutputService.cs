using System;
using System.Threading.Tasks;
using NBitcoin;

namespace Core.Outputs
{
    public interface ISpentOutputService
    {
        Task SaveSpentOutputs(Guid transactionId, Transaction transaction);

        Task RemoveSpentOutputs(Transaction transaction);
    }
}

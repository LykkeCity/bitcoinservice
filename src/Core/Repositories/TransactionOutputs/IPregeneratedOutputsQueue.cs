using System.Threading.Tasks;
using NBitcoin;

namespace Core.Repositories.TransactionOutputs
{
    public interface IPregeneratedOutputsQueue
    {
        Task<Coin> DequeueCoin();
        Task EnqueueOutputs(params Coin[] coins);
        Task<int> Count();
    }
}

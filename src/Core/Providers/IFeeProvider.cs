using System.Threading.Tasks;
using NBitcoin;

namespace Core.Providers
{
    public interface IFeeProvider
    {
        Task<Money> CalcFeeForTransaction(TransactionBuilder builder);
        Task<Money> CalcFeeForTransaction(Transaction builder);
        Task<FeeRate> GetFeeRate();
        Task<Money> CalcFee(int size);
    }
}

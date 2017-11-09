using System.Threading.Tasks;
using NBitcoin;

namespace Core.Providers
{
    public interface IFeeProvider
    {
        Task<Money> CalcFeeForTransaction(TransactionBuilder builder, decimal? feeMultiplier = null);
        Task<Money> CalcFeeForTransaction(Transaction builder, int feeRate = 0);
        Task<FeeRate> GetFeeRate(decimal? feeMultiplier = null);
        Task<Money> CalcFee(int size);
    }
}

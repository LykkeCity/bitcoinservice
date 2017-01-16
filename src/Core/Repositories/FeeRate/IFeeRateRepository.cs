using System.Threading.Tasks;

namespace Core.Repositories.FeeRate
{
    public interface IFeeRate
    {
        int FeeRate { get; }
    }

    public interface IFeeRateRepository
    {
        Task UpdateFeeRate(int fee);
        Task<int> GetFeePerByte();
    }
}

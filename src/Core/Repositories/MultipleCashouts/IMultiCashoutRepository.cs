using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;

namespace Core.Repositories.MultipleCashouts
{
    public interface IMultiCashoutRepository
    {
        Task<IMultipleCashout> GetCurrentMultiCashout();

        Task CloseMultiCashout(Guid multicashoutId);

        Task CompleteMultiCashout(Guid multicashoutId);
        Task CreateMultiCashout(Guid multiCashoutId, string hex, string txHash);
        Task IncreaseTryCount(Guid multiCashoutId);
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories.MultipleCashouts
{
    public interface ICashoutRequestRepository
    {
        Task<ICashoutRequest> CreateCashoutRequest(Guid id, decimal amount, string destination);        

        Task<IEnumerable<ICashoutRequest>> GetOpenRequests();
        Task<IEnumerable<ICashoutRequest>> GetCashoutRequests(Guid multipleCashoutId);
        Task SetMultiCashoutId(IEnumerable<Guid> cashoutIds, Guid multiCashoutId);
    }
}

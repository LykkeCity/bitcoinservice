using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories.TransactionOutputs
{
    public interface IInternalSpentOutput
    {
        string TransactionHash { get; set; }
        int N { get; set; }
    }


    public interface IInternalSpentOutputRepository
    {
        Task<IEnumerable<IInternalSpentOutput>> GetInternalSpentOutputs();
        Task Insert(string transactionHash, int n);
    }
    
}

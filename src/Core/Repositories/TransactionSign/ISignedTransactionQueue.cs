using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.TransactionSign
{
    public interface ISignedTransactionQueue
    {
        Task AddMessageAsync(string msg);
    }
}

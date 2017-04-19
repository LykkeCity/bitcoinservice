using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;

namespace Core.Repositories.TransactionOutputs
{
    public interface INinjaOutputBlobStorage
    {
        Task Save(string address, IEnumerable<ICoin> coins);
    }
}

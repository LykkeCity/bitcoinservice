using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.TransactionOutputs
{
    public interface IPregeneratedOutputsQueueFactory
    {
        IPregeneratedOutputsQueue Create(string assetId);
        IPregeneratedOutputsQueue CreateFeeQueue();
    }
}

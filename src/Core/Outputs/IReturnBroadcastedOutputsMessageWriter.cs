using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Outputs
{
    public interface IReturnBroadcastedOutputsMessageWriter
    {
        Task AddToReturn(string transactionHex, string address);
    }
}

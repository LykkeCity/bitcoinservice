using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Outputs
{
    public interface IReturnOutputsMessageWriter
    {
        Task AddToReturn(string transactionHex, List<string> address);
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;

namespace Core.Bcc
{
    public interface IBccOutputService
    {
        Task<IEnumerable<ICoin>> GetUnspentOutputs(string walletAddress);
    }
}

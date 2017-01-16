using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Providers
{
    public interface ISignatureApiProvider
    {
        Task<string> GeneratePubKey();

        Task<string> SignTransaction(string transactionHex);
    }
}

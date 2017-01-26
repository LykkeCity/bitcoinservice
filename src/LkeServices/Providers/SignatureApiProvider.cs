using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Providers;

namespace LkeServices.Providers
{
    public class SignatureApiProvider : ISignatureApiProvider
    {
        private readonly ISignatureApi _signatureApi;

        public SignatureApiProvider(ISignatureApi signatureApi)
        {
            _signatureApi = signatureApi;
        }

        public async Task<string> GeneratePubKey()
        {
            return (await _signatureApi.GeneratePubKey()).PubKey;
        }

        public async Task<string> SignTransaction(string transaction)
        {
            return (await _signatureApi.SignTransaction(new TransactionSignRequest(transaction))).SignedTransaction;
        }
    }
}

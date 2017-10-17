using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Providers;
using NBitcoin;

namespace LkeServices.Providers
{
    public class SignatureApiProvider : ISignatureApiProvider
    {
        private readonly ISignatureApi _signatureApi;

        public SignatureApiProvider(ISignatureApi signatureApi)
        {
            _signatureApi = signatureApi;
        }

        public async Task<string> GeneratePubKey(string tag = null)
        {
            return (await _signatureApi.GeneratePubKey(tag)).PubKey;
        }

        public async Task<PubKeyResponse> GenerateWallet(string tag = null)
        {
            return await _signatureApi.GeneratePubKey(tag);
        }

        public async Task<string> SignTransaction(string transaction, SigHash hashType = SigHash.All, string[] additionalSecrets = null, string[] prevTransactions = null)
        {
            return (await _signatureApi.SignTransaction(new TransactionSignRequest(transaction, (byte)hashType, additionalSecrets, prevTransactions))).SignedTransaction;
        }

        public async Task<string> SignBccTransaction(string transaction, SigHash hashType = SigHash.All, string[] additionalSecrets = null)
        {
            return (await _signatureApi.SignBccTransaction(new TransactionSignRequest(transaction, (byte)hashType, additionalSecrets))).SignedTransaction;
        }

        public async Task<string> GetNextAddress(string address)
        {
            return (await _signatureApi.GetNextAddress(new NextAddressRequest {Address = address})).Address;            
        }

        public Task AddKey(string privateKey)
        {
            return _signatureApi.AddKey(new AddKeyRequest { Key = privateKey });
        }
    }
}

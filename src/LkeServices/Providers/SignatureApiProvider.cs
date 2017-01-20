using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Core.Providers;
using Core.Settings;
using RestSharp;
using Common;
using Core.Repositories.ApiRequests;

namespace LkeServices.Providers
{
    public enum SignatureApiProviderType
    {
        Client,
        Exchange
    }

    public class SignatureApiProvider : BaseApiProvider, ISignatureApiProvider
    {

        public SignatureApiProvider(string url, ILog logger, IRestClient restClient, IApiRequestBlobRepository apiBlob)
            : base(url, restClient, logger, apiBlob)
        {
        }

        public async Task<string> GeneratePubKey()
        {
            var request = new RestRequest("/api/bitcoin/key", Method.GET);

            return (await DoRequest<PubKeyResponse>(request)).PubKey;
        }

        public async Task<string> SignTransaction(string transactionHex)
        {
            var request = new RestRequest("/api/bitcoin/sign", Method.POST);

            request.AddJsonBody(new
            {
                Transaction = transactionHex
            });

            return (await DoRequest<TransactionResponse>(request)).SignedTransaction;
        }
    }

    public class PubKeyResponse
    {
        public string PubKey { get; set; }
    }

    public class TransactionResponse
    {
        public string SignedTransaction { get; set; }
    }
}

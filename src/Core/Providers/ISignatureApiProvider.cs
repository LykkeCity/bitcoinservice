using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestEase;

namespace Core.Providers
{

    public interface ISignatureApi
    {
        [Get("/api/bitcoin/key")]
        Task<PubKeyResponse> GeneratePubKey();

        [Post("/api/bitcoin/sign")]
        Task<TransactionResponse> SignTransaction([Body] TransactionSignRequest request);

        [Get("/api/IsAlive")]
        Task IsAlive();
    }

    public interface ISignatureApiProvider
    {        
        Task<string> GeneratePubKey();
        
        Task<string> SignTransaction(string transaction);
    }

    public class TransactionSignRequest
    {
        public TransactionSignRequest(string transaction)
        {
            Transaction = transaction;
        }

        public string Transaction { get; set; }
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

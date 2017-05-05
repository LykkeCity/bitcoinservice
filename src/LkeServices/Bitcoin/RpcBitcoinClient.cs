using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.Bitcoin;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Transactions;
using NBitcoin;
using NBitcoin.RPC;

namespace LkeServices.Bitcoin
{
    public class RpcBitcoinClient : IRpcBitcoinClient
    {
        private readonly IBroadcastedTransactionRepository _broadcastedTransactionRepository;
        private readonly IBroadcastedTransactionBlobStorage _broadcastedTransactionBlob;
        private readonly RPCClient _client;

        public RpcBitcoinClient(RpcConnectionParams connectionParams, IBroadcastedTransactionRepository broadcastedTransactionRepository, IBroadcastedTransactionBlobStorage broadcastedTransactionBlob)
        {
            _broadcastedTransactionRepository = broadcastedTransactionRepository;
            _broadcastedTransactionBlob = broadcastedTransactionBlob;
            _client = new RPCClient(new NetworkCredential(connectionParams.UserName, connectionParams.Password), connectionParams.IpAddress, connectionParams.Network);
        }

        public async Task BroadcastTransaction(Transaction tr, Guid transactionId)
        {
            await _client.SendRawTransactionAsync(tr);

            await Task.WhenAll(
                _broadcastedTransactionRepository.InsertTransaction(tr.GetHash().ToString(), transactionId),
                _broadcastedTransactionBlob.SaveToBlob(transactionId, tr.ToHex())
            );
        }

        public async Task<string> GetTransactionHex(string trId)
        {
            return (await _client.GetRawTransactionAsync(uint256.Parse(trId))).ToHex();
        }

        public Task<int> GetBlockCount()
        {
            return _client.GetBlockCountAsync();
        }
    }
}

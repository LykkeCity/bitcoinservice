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
        private readonly RPCClient _client;

        public RpcBitcoinClient(RpcConnectionParams connectionParams, IBroadcastedTransactionRepository broadcastedTransactionRepository)
        {
            _broadcastedTransactionRepository = broadcastedTransactionRepository;
            _client = new RPCClient(new NetworkCredential(connectionParams.UserName, connectionParams.Password), connectionParams.IpAddress, connectionParams.Network);
        }

        public async Task BroadcastTransaction(Transaction tr)
        {
            await _client.SendRawTransactionAsync(tr);

            await _broadcastedTransactionRepository.InsertTransaction(tr.GetHash().ToString());
        }

        public Task BroadcastTransaction(string tr)
        {
            return BroadcastTransaction(new Transaction(tr));
        }

        public async Task<string> GetTransactionHex(string trId)
        {
            return (await _client.GetRawTransactionAsync(uint256.Parse(trId))).ToHex();
        }
    }
}

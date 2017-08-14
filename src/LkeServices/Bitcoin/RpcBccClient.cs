using System;
using System.Collections.Generic;
using System.Text;
using Autofac.Features.AttributeFilters;
using Core;
using Core.Bitcoin;
using Core.Repositories.Transactions;

namespace LkeServices.Bitcoin
{
    public class RpcBccClient : RpcBitcoinClient
    {
        public RpcBccClient([KeyFilter(Constants.BccKey)] RpcConnectionParams connectionParams, IBroadcastedTransactionRepository broadcastedTransactionRepository, IBroadcastedTransactionBlobStorage broadcastedTransactionBlob) : 
            base(connectionParams, broadcastedTransactionRepository, broadcastedTransactionBlob)
        {
        }
    }
}

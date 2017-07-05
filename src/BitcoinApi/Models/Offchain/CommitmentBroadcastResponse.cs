using System;
using System.Collections.Generic;
using System.Text;
using LkeServices.Transactions;

namespace BitcoinApi.Models.Offchain
{
    public class CommitmentBroadcastResponse
    {
        public List<CommitmentBroadcastInfo> CommitmentBroadcasts { get; set; }
    }
}

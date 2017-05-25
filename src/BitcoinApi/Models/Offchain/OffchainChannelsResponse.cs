using System;
using System.Collections.Generic;
using System.Text;
using LkeServices.Transactions;

namespace BitcoinApi.Models.Offchain
{
    public class OffchainChannelsResponse
    {
        public IEnumerable<OffchainChannelInfo> Channels { get; set; }     
    }
}

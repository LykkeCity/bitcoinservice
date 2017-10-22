﻿using System;
using System.Collections.Generic;
using System.Text;
using Bitcoin.Api.Client.BitcoinApi.Models;

namespace Core.BitCoin.BitcoinApi.Models
{    

    public class BccSplitTransactionResponse : Response
    {
        public string Transaction { get; set; }

        public decimal ClientAmount { get; set; }

        public decimal HubAmount { get; set; }

        public decimal ClientFeeAmount { get; set; }

        public string Outputs { get; set; }
    }

    public class BccTransactionHashResponse : Response
    {
        public string TransactionHash { get; set; }
    }

    public class BccTransactionResponse : Response
    {
        public string Transaction { get; set; }
    }
}

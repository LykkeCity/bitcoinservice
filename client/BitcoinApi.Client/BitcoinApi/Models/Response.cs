using System;
using System.Collections.Generic;
using System.Text;
using Core.BitCoin.BitcoinApi.Models;

namespace Bitcoin.Api.Client.BitcoinApi.Models
{
    public class Response
    {
        public ErrorResponse Error { get; set; }

        public bool HasError => Error != null;
    }
}

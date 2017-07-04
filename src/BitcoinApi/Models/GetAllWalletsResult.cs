using System;
using System.Collections.Generic;
using System.Text;

namespace BitcoinApi.Models
{
    public class GetAllWalletsResult
    {
        public IEnumerable<string> Multisigs { get; set; }
    }
}

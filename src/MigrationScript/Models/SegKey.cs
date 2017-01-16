using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MigrationScript.Models
{
    public class SegKey
    {
        [Key]
        public long Id { get; set; }
        public string ClientPubKey { get; set; }
        public string ExchangePrivateKey { get; set; }
        public string ClientAddress { get; set; }
        public string MultiSigAddress { get; set; }
    }
}

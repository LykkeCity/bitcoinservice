using System.Collections.Generic;

namespace Lykke.Bitcoin.Api.Client.BitcoinApi.Models
{
    public class AllWalletsResponse : Response
    {
        public List<string> Multisigs { get; set; }
    }

    public class Wallet : Response
    {
        public string Multisig { get; set; }

        public string ColoredMultisig { get; set; }
    }

    public class LykkePayWallet : Response
    {
        public string Address { get; set; }

        public string PubKey { get; set; }

        public string Tag { get; set; }
    }

    public class SegwitWallet : Response
    {
        public string Address { get; set; }
    }

}

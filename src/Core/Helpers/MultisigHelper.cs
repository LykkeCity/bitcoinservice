using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;

namespace Core.Helpers
{
    public static class MultisigHelper
    {
        public static BitcoinScriptAddress GenerateMultisig(string pubKey1, string pubKey2, Network network)
        {
            var redeemScrip = GenerateMultisigRedeemScript(pubKey1, pubKey2);
            return redeemScrip.GetScriptAddress(network);
        }


        public static Script GenerateMultisigRedeemScript(string pubKey1, string pubKey2)
        {
            return PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, new PubKey(pubKey1), new PubKey(pubKey2));            
        }



    }
}

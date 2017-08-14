using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace Core.Repositories.TransactionOutputs
{
    public class BroadcastedOutput : IBroadcastedOutput
    {
        public string AssetId { get; set; }

        public Guid TransactionId { get; }

        public string Address { get; set; }

        public string ScriptPubKey { get; set; }

        public long Amount { get; set; }

        public long Quantity { get; set; }

        public string TransactionHash { get; set; }
        public int N { get; set; }


        private BroadcastedOutput(ICoin coin, Network net)
        {
            Address = coin.TxOut.ScriptPubKey.GetDestinationAddress(net).ToString();
            ScriptPubKey = coin.TxOut.ScriptPubKey.ToHex();
            N = (int)coin.Outpoint.N;
            var coin1 = coin as Coin;
            if (coin1 != null)
                Amount = coin1.Amount.Satoshi;

            var colorCoin = coin as ColoredCoin;
            if (colorCoin != null)
            {
                Amount = colorCoin.Bearer.Amount.Satoshi;
                Quantity = colorCoin.Amount.Quantity;
                AssetId = colorCoin.AssetId.GetWif(net).ToString();
            }
        }

        public BroadcastedOutput(ICoin coin, Guid transactionId, Network net) : this(coin, net)
        {
            TransactionId = transactionId;
        }

        public BroadcastedOutput(ICoin coin, string transactionHash, Network net) : this(coin, net)
        {
            TransactionId = Guid.NewGuid();
            TransactionHash = transactionHash;
        }
    }
}

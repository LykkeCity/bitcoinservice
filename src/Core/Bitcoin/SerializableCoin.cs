using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Helpers;
using NBitcoin;

namespace Core.Bitcoin
{
    public class SerializableCoin
    {
        public string TransactionHash { get; set; }

        public uint N { get; set; }

        public string ScriptPubKey { get; set; }

        public long Amount { get; set; }


        public SerializableCoin()
        {

        }
        public SerializableCoin(Coin coin)
        {
            TransactionHash = coin.Outpoint.Hash.ToString();
            N = coin.Outpoint.N;
            ScriptPubKey = coin.ScriptPubKey.ToHex();
            Amount = coin.Amount.Satoshi;
        }

        public Coin ToCoin()
        {
            return new Coin(new OutPoint(uint256.Parse(TransactionHash), N), new TxOut(Amount, ScriptPubKey.ToScript()));
        }
    }
}

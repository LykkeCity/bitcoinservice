using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Bitcoin.Tests;
using Core.Bitcoin;
using Core.Helpers;
using Core.OpenAssets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;

namespace tests
{
    [TestClass]
    public class SegwitExamples
    {
        [TestMethod]
        public void CreateSegwitOverP2ShMultisig()
        {
            var pubKey1 = new PubKey("03ebcc2d675d17c5b5e250307cb0189bfc5adf6809bfd3c2823a2884dbbcaec58b").Compress();
            var pubKey2 = new PubKey("02235060021d06f6c4e766574b0374dde8d050a0a036ee52cde04608a87eebc3e1").Compress();

            var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, pubKey1, pubKey2);
            var witness = redeem.WitHash;

            //p2wsh over p2sh
            var addr = witness.ScriptPubKey.Hash.GetAddress(Network.TestNet);
        }

        [TestMethod]
        public void CreateSegwitMultisig()
        {
            var pubKey1 = new PubKey("03ebcc2d675d17c5b5e250307cb0189bfc5adf6809bfd3c2823a2884dbbcaec58b").Compress();
            var pubKey2 = new PubKey("02235060021d06f6c4e766574b0374dde8d050a0a036ee52cde04608a87eebc3e1").Compress();

            var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, pubKey1, pubKey2);
            var witness = redeem.WitHash;

            //p2wsh over p2sh
            var addr = witness.GetAddress(Network.TestNet).ToString();
        }

        [TestMethod]
        public async Task SpendSegwitOverP2ShOutput()
        {
            var pk1 = Key.Parse("cMahea7zqjxrtgAbB7LSGbcZDo359LNtib5kYpwbiSqBqvs6cqPV");
            var pk2 = Key.Parse("cQDux9gANFC1mPiwPpx7feHpiZu9xKn8RyV8yLErazuzWt146oY1");
            var pubKey1 = new PubKey("03ebcc2d675d17c5b5e250307cb0189bfc5adf6809bfd3c2823a2884dbbcaec58b").Compress();
            var pubKey2 = new PubKey("02235060021d06f6c4e766574b0374dde8d050a0a036ee52cde04608a87eebc3e1").Compress();

            var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, pubKey1, pubKey2);

            var coin = new Coin(new OutPoint(uint256.Parse("2b2e47cd0ba3be2a013fd8658f24e66aae5d492837423ccee76e4e670e980b6f"), 0),
                new TxOut(Money.FromUnit(1, MoneyUnit.BTC), "a914d388706006374eb728135014c9cad72e5dcd72fe87".ToScript()));

            var scriptCoin = coin.ToScriptCoin(redeem);

            var builder = new TransactionBuilder();
            builder.AddCoins(scriptCoin);
            builder.Send(OpenAssetsHelper.GetBitcoinAddressFormBase58Date("mj5FEqrC2P4FjFNfX8q3eZ4UABWUcRNy9r"), Money.FromUnit(0.5M, MoneyUnit.BTC));
            builder.SetChange(coin.ScriptPubKey);
            builder.SendFees(Money.FromUnit(0.0001M, MoneyUnit.BTC));

            builder.AddKeys(pk1, pk2);
            var tr = builder.BuildTransaction(true);

            //await Broadcast(tr);
        }

        private static async Task Broadcast(Transaction tr)
        {
            var rpcClient = Config.Services.GetService<IRpcBitcoinClient>();
            await rpcClient.BroadcastTransaction(tr, Guid.NewGuid());
        }

        [TestMethod]
        public async Task SendToWitnessAddress()
        {
            var sourceAddr = OpenAssetsHelper.GetBitcoinAddressFormBase58Date("mj5FEqrC2P4FjFNfX8q3eZ4UABWUcRNy9r");
            var addr = OpenAssetsHelper.GetBitcoinAddressFormBase58Date("tb1q2xvc2c503h95sm8nu5wyj68xee23su5wt46au5ztdsa9neqs424qh8kxal");
            var coin = new Coin(new OutPoint(uint256.Parse("1fcedb863194c3c31f133e09c509e54b2cb40ef0a651a41a23d534301eb57af0"), 1),
                new TxOut(Money.FromUnit(0.5M, MoneyUnit.BTC), sourceAddr));
            var key = Key.Parse("93586ks3uwSAgJ6q3He4CkuXeVg1N4syvszP514TitfcA9mXjVo");
            var builder = new TransactionBuilder();
            builder.AddCoins(coin);
            builder.SetChange(sourceAddr);
            builder.SendFees(Money.FromUnit(0.0001M, MoneyUnit.BTC));
            builder.Send(addr, Money.FromUnit(0.2M, MoneyUnit.BTC));
            builder.AddKeys(key);
            var tr = builder.BuildTransaction(true);
            //await Broadcast(tr);
        }


        [TestMethod]
        public async Task SpendSegwitOutput()
        {
            var pk1 = Key.Parse("cMahea7zqjxrtgAbB7LSGbcZDo359LNtib5kYpwbiSqBqvs6cqPV");
            var pk2 = Key.Parse("cQDux9gANFC1mPiwPpx7feHpiZu9xKn8RyV8yLErazuzWt146oY1");
            var pubKey1 = new PubKey("03ebcc2d675d17c5b5e250307cb0189bfc5adf6809bfd3c2823a2884dbbcaec58b").Compress();
            var pubKey2 = new PubKey("02235060021d06f6c4e766574b0374dde8d050a0a036ee52cde04608a87eebc3e1").Compress();

            var redeem = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, pubKey1, pubKey2);

            var coin = new Coin(new OutPoint(uint256.Parse("9aff0ea55be499f0c664e447a93f7a38a87cc34f24f464642c7553b40fd18958"), 1),
                new TxOut(Money.FromUnit(0.2M, MoneyUnit.BTC), redeem.WitHash.ScriptPubKey));

            var scriptCoin = coin.ToScriptCoin(redeem);

            var builder = new TransactionBuilder();
            builder.AddCoins(scriptCoin);
            builder.Send(OpenAssetsHelper.GetBitcoinAddressFormBase58Date("mj5FEqrC2P4FjFNfX8q3eZ4UABWUcRNy9r"), Money.FromUnit(0.1M, MoneyUnit.BTC));
            builder.SetChange(coin.ScriptPubKey);
            builder.SendFees(Money.FromUnit(0.0001M, MoneyUnit.BTC));

            builder.AddKeys(pk1, pk2);
            var tr = builder.BuildTransaction(true);

            //await Broadcast(tr);
        }

    }
}

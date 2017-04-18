using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Core.Bitcoin;
using Core.OpenAssets;
using Core.Providers;
using Core.QBitNinja;
using Core.Repositories.Assets;
using Core.ScriptTemplates;
using LkeServices.Providers;
using LkeServices.Transactions;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;
using NUnit.Framework;
using PhoneNumbers;

namespace Bitcoin.Tests
{
    [TestFixture]
    public class BitcoinTransactionTests
    {
        [Test]
        public void GetInfoTest()
        {
            var address1 = new PubKey("03676601a24b05f5652ef4ce3616f505541d42072b28057007f43e0874bb27e47d").GetAddress(Network.TestNet);
            var address2 = new PubKey("03ebcc2d675d17c5b5e250307cb0189bfc5adf6809bfd3c2823a2884dbbcaec58b").GetAddress(Network.TestNet);
            var address3 = new PubKey("04b1ce3afeb590c6a5295475e0eb91bc10feb9ccc3277d7f0e603059cd6a6d35a6eac01d3772af5251dafad36d5004ff839d5d4d49fa5386dd57a614188f4a84ee").GetAddress(Network.TestNet);

            var key = new Key();

            var pk = new BitcoinSecret("93586ks3uwSAgJ6q3He4CkuXeVg1N4syvszP514TitfcA9mXjVo", Network.TestNet);
            //{0491590ab4eb4b5227b82dbdb8fbf5dc850325b953a2990036e33a5f538ff17acd1d345ce3199b55a4ebc111addb1b824c4aeafc97ecffed280f1cd0fd009dd8d7}
            var pubKey = pk.PubKey;

            var script = CreateScript(new PubKey("02483c13420c9eec1846a6878fc0b41f481d4e9a66fc8038d48543a50053a71608"), new PubKey("0491590ab4eb4b5227b82dbdb8fbf5dc850325b953a2990036e33a5f538ff17acd1d345ce3199b55a4ebc111addb1b824c4aeafc97ecffed280f1cd0fd009dd8d7"), new PubKey("0491590ab4eb4b5227b82dbdb8fbf5dc850325b953a2990036e33a5f538ff17acd1d345ce3199b55a4ebc111addb1b824c4aeafc97ecffed280f1cd0fd009dd8d7"));

            var tr = new Transaction("010000000406e224b39f9df7c6f9377bc3cd5dc8193c5485b81d0852048b9b73b50893492201000000da00473044022038f11b0f5209f8f3dadee6e2dbbdde4246a77aeaac1204d71246dac2455f7beb02205eba62b53216dd23c444074994f7a9a6ea1731c97308daff976ca12374f68a3801483045022100ac7009ca8237ad4443c92ab769e99e644a01f047b072bf4d84da07b057e2516102202457848b173ea729103be42abf528fd09d695d40207b04915e74a36e17b441fe01475221034b3e91516c72c41b603bc1100d495875a130a7c939fe798fd08de04730c2a7e52103efced59f08b1aea316852817aa3cf5e80667a7750739bc6c76b8df0cd228570b52aefffffffff397f641fe63f4a3fb79e9d1b75164ab78563a486085a890712b6ed0e14b621c01000000da00483045022100e09a8a2003a5d7e183a9fc8709452efe1cc53c9209451bf08f20c907a5f16e8e02204883dc3c483da48bbe3ffa64b4da6bac47cf9a10d1438314bfad4b9f511a34690147304402207bc09970f4a08dfc634dc02e71b8e83402d4398f87a4895de80ec2136ab92d18022004e95904d1c2c87cb2c0c3f0525de9233e812b81e76dc1c382d75f3fe34afecb01475221034b3e91516c72c41b603bc1100d495875a130a7c939fe798fd08de04730c2a7e52103efced59f08b1aea316852817aa3cf5e80667a7750739bc6c76b8df0cd228570b52aeffffffff459f84cd7aa25c64da33b854736223d7ad10672c67ca818cdb9a689e82ed79e5330000008a47304402202b672b2f2550a484a3c2975ad7ef2182b4c9f603f434d6a3a3a4a2cbe43a9f7f02205cb24e6a832a908000525593a8f0362d4b9182d29014625676147e236e4bc720014104ff20028f41de7e2bac4f8e90884becad36c1390d2ab991a16fbcf745db478fd37cbf65c57a84e5a485bd5934c659f94aff35fe9fc50ebd2281ede40366190f57ffffffff459f84cd7aa25c64da33b854736223d7ad10672c67ca818cdb9a689e82ed79e5340000008a47304402207516c51652781df0d13818ca8c72e1dbd26db11be42f6e5f54ee67e0462985e302203a1cd6c15e718b3ba77bab0a32384d2a9da3ab5a94f2f4338646f428a644ab41014104ff20028f41de7e2bac4f8e90884becad36c1390d2ab991a16fbcf745db478fd37cbf65c57a84e5a485bd5934c659f94aff35fe9fc50ebd2281ede40366190f57ffffffff0300000000000000000b6a094f41010002904e00008c0a00000000000017a9146926204962560bea19baf55e0f947f0a21a421a287b48c0000000000001976a914ed75405f426601f5493117b5a22dc0082269e32288ac00000000");
            var marker = tr.GetColoredMarker();
        }

        [Test]
        public async Task TestBroadcastCommitment()
        {
            var rpcClient = Config.Services.GetService<IRpcBitcoinClient>();
            var trService = Config.Services.GetService<IBitcoinTransactionService>();
            var helper = Config.Services.GetService<ITransactionBuildHelper>();

            var hex =
                "01000000030fc8d9f48344cb9496af8b77c24b6d36bff11041d37b8ee0ac5c8c8aa63e0292020000004b00000047522102593e745d594696f503acab8d8a20fe6a9b97e9f63873e124ac79113dd20a03ae210379be0eea5380e3b03ae31efcfb19eba41fa6e6fdd0dc3329d4d658eca25168f452aeffffffff399b06f441db3581576da93139700d176bd726f54e05f51ace6dfedf68ada123040000004b000000475221025a4cd2d3e5e12142df245a8bc24fa9ac0a6cb412a205ca3d31b1fef891126cbf21020f0efab8a2845a8030b3a6eb535577fd38364f0897ca32ebd956223653cc6da252aeffffffff6735d37552e63c0d1386c3bd40d687546c917326502aafbb31f8018909e41a943300000000ffffffff0600000000000000000d6a0b4f41010004310538b006008c0a00000000000017a914745d46755d712e8ec662a30f70c4830261750932878c0a00000000000017a914da617d341e35dde727780f277129df8538be7f54878c0a00000000000017a914da617d341e35dde727780f277129df8538be7f54878c0a00000000000017a914745d46755d712e8ec662a30f70c483026175093287d0200000000000001976a9144104da83ef80ce0b2e5843230f489f1455e98ef688ac00000000";
            var tr = new Transaction(hex);

            var source = BitcoinAddress.Create("mj5FEqrC2P4FjFNfX8q3eZ4UABWUcRNy9r");
            var dest = BitcoinAddress.Create("2N3QKLu6mmH9ZrvRQdf5pNg1txeTpwLhjVK");
            var bitcoinOutService = Config.Services.GetService<IBitcoinOutputsService>();
            var assetRepo = Config.Services.GetService<IAssetRepository>();
            var usd = await assetRepo.GetAssetById("USD");
            var lkk = await assetRepo.GetAssetById("LKK");


            var usdID = new BitcoinAssetId(usd.BlockChainAssetId).AssetId;

            var lkkID = new BitcoinAssetId(lkk.BlockChainAssetId).AssetId;
            var usdCoins = (await bitcoinOutService.GetColoredUnspentOutputs("mj5FEqrC2P4FjFNfX8q3eZ4UABWUcRNy9r",
                usdID)).ToList();
            var lkkCoins = (await bitcoinOutService.GetColoredUnspentOutputs("mj5FEqrC2P4FjFNfX8q3eZ4UABWUcRNy9r",
               lkkID)).ToList();

            TransactionBuilder builder = new TransactionBuilder();
            TransactionBuildContext context = new TransactionBuildContext(Network.TestNet, null, null);
            helper.SendAssetWithChange(builder, context, usdCoins, dest, new AssetMoney(usdID, 100), source);
            helper.SendAssetWithChange(builder, context, lkkCoins, dest, new AssetMoney(lkkID, 10000), source);
            await helper.AddFee(builder, context);

            var tx = builder.BuildTransaction(true).ToHex();

            //var multisigScriptOps = PayToMultiSigTemplate.Instance.GenerateScriptPubKey
            //(2,
            //    new PubKey[]
            //    {
            //        new PubKey("03ebcc2d675d17c5b5e250307cb0189bfc5adf6809bfd3c2823a2884dbbcaec58b"),
            //        new PubKey("02235060021d06f6c4e766574b0374dde8d050a0a036ee52cde04608a87eebc3e1")
            //    }).ToOps().ToList();

            //var redeem =
            //    "2 03ebcc2d675d17c5b5e250307cb0189bfc5adf6809bfd3c2823a2884dbbcaec58b 02235060021d06f6c4e766574b0374dde8d050a0a036ee52cde04608a87eebc3e1 2 OP_CHECKMULTISIG";
            //var pubkeys = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(new Script(redeem)).PubKeys;



        }

        [Test]
        public async Task TestScript()
        {
            var pk = new BitcoinSecret("93586ks3uwSAgJ6q3He4CkuXeVg1N4syvszP514TitfcA9mXjVo");
            var pk2 = new BitcoinSecret("cPsQrkj1xqQUomDyDHXqsgnjCbZ41yfr923tWR7EuaSKH7WtDXb9");
            var address = "mj5FEqrC2P4FjFNfX8q3eZ4UABWUcRNy9r";

            var bitcoinOutService = Config.Services.GetService<IBitcoinOutputsService>();
            var helper = Config.Services.GetService<ITransactionBuildHelper>();
            var signProvider = Config.Services.GetService<Func<SignatureApiProviderType, ISignatureApiProvider>>()(SignatureApiProviderType.Exchange);
            var bitcoinClient = Config.Services.GetService<IRpcBitcoinClient>();
            var outputs = await bitcoinOutService.GetUncoloredUnspentOutputs(address);


            TransactionBuilder builder = new TransactionBuilder();

            //helper.SendWithChange(builder, context, outputs.ToList(),
            //    CreateScript(pk.PubKey.ToHex(), pk2.PubKey.ToHex()).GetScriptAddress(Network.TestNet),
            //    new Money(0.1M, MoneyUnit.BTC), new BitcoinPubKeyAddress(address));
            //await helper.AddFee(builder, context);

            var tr = builder.BuildTransaction(true);

            var signed = await signProvider.SignTransaction(tr.ToHex());

            await bitcoinClient.BroadcastTransaction(new Transaction(signed), Guid.NewGuid());
        }

        [Test]
        public async Task TestSpendScript()
        {

            var multisigFirstPartPk = new BitcoinSecret("cMahea7zqjxrtgAbB7LSGbcZDo359LNtib5kYpwbiSqBqvs6cqPV");
            var singlePk = new BitcoinSecret("cPsQrkj1xqQUomDyDHXqsgnjCbZ41yfr923tWR7EuaSKH7WtDXb9");
            var revokePk = new BitcoinSecret("cPsQrkj1xqQUomDyDHXqsgnjCbZ41yfr923tWR7EuaSKH7WtDXb9");


            var bitcoinClient = Config.Services.GetService<IRpcBitcoinClient>();
            var bitcoinOutService = Config.Services.GetService<IBitcoinOutputsService>();
            var assetRepo = Config.Services.GetService<IAssetRepository>();
            var asset = await assetRepo.GetAssetById("USD");
            var helper = Config.Services.GetService<ITransactionBuildHelper>();

            var coin = (await bitcoinOutService.GetUnspentOutputs("2MuoR6cZxpYWEj2RVefs4xRzqy4VGzpBU5B"))
                .OfType<ColoredCoin>().First();

            var redeem = CreateScript(multisigFirstPartPk.PubKey, revokePk.PubKey, singlePk.PubKey);

            var scriptCoin = new ScriptCoin(coin, redeem).ToColoredCoin(coin.Amount);

            TransactionBuilder builder = new TransactionBuilder();
            TransactionBuildContext context = new TransactionBuildContext(Network.TestNet, null, null);

            //builder.AddKeys(pk);
            builder.AddCoins(scriptCoin);

            builder.SendAsset(new BitcoinSecret("93586ks3uwSAgJ6q3He4CkuXeVg1N4syvszP514TitfcA9mXjVo").PubKey.GetAddress(Network.TestNet),
                new AssetMoney(new BitcoinAssetId(asset.BlockChainAssetId), 100));
            builder.SetChange(new BitcoinScriptAddress("2MuoR6cZxpYWEj2RVefs4xRzqy4VGzpBU5B", Network.TestNet));

            await helper.AddFee(builder, context);
            var tr = builder.BuildTransaction(false);


            //tr.Inputs[0].Sequence = new Sequence(144);
            // tr.Version = 2;
            var hash = Script.SignatureHash(redeem, tr, 0, SigHash.All);

            var signature = singlePk.PrivateKey.Sign(hash, SigHash.All).Signature.ToDER().Concat(new byte[] { 0x01 }).ToArray();

            var push1 = multisigFirstPartPk.PrivateKey.Sign(hash, SigHash.All).Signature.ToDER().Concat(new byte[] { 0x01 }).ToArray();
            var push2 = revokePk.PrivateKey.Sign(hash, SigHash.All).Signature.ToDER().Concat(new byte[] { 0x01 }).ToArray();

            var scriptParams = new OffchainScriptParams
            {
                IsMultisig = true,
                RedeemScript = redeem.ToBytes(),
                Pushes = new[] { new byte[0], push1, push2 }
            };

            tr.Inputs[0].ScriptSig = OffchainScriptCommitmentTemplate.GenerateScriptSig(scriptParams);


            ScriptError error;
            tr.Inputs.AsIndexedInputs().First().VerifyScript(scriptCoin.ScriptPubKey, out error);
            await bitcoinClient.BroadcastTransaction(tr, Guid.NewGuid());

            //CheckSequence(1, tr, 0);
            //CheckSig(signature.ToBytes(), pk.PubKey.ToBytes(), redeem, new TransactionChecker(tr, 0, scriptCoin.Amount), 0);
        }

        private Script CreateScript(PubKey pbk1, PubKey pbk2, PubKey singlePk)
        {
            var multisigScriptOps = PayToMultiSigTemplate.Instance.GenerateScriptPubKey
               (2, pbk1, pbk2).ToOps();
            List<Op> ops = new List<Op>();

            ops.Add(OpcodeType.OP_IF);
            ops.AddRange(multisigScriptOps);
            ops.Add(OpcodeType.OP_ELSE);
            ops.Add(Op.GetPushOp(144));
            ops.Add(OpcodeType.OP_CHECKSEQUENCEVERIFY);
            ops.Add(OpcodeType.OP_DROP);
            ops.Add(Op.GetPushOp(singlePk.ToBytes()));
            ops.Add(OpcodeType.OP_CHECKSIG);
            ops.Add(OpcodeType.OP_ENDIF);
            return new Script(ops.ToArray());
        }


    }
}

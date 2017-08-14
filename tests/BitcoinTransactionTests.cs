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
using Core.Repositories.TransactionOutputs;
using Core.ScriptTemplates;
using LkeServices.Providers;
using LkeServices.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;
using PhoneNumbers;

namespace Bitcoin.Tests
{
    [TestClass]
    public class BitcoinTransactionTests
    {
        [TestMethod]
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

            var tr = new Transaction("0100000002c1d5276abf671bdb7383f925e2a227432c3458fa53c167a3fc789a62ce82564a010000006b483045022100891301b4f49a98b44898026c92d70e54a8b7ac7fb5585bacf810b28a398d73c502206b7c23dece57a306d306dbbb65edb65f8f7b02ddabc328638323583079f55b5e012102626524cabfc3d8102941d04d02e002b87886b61aad3dae2f1398ac5f5c58d8f9ffffffff14008514e19c4d66b04375760417c96d9ea745a07e0c7a07265e9c64d53f4cef4f0000006b483045022100c867a7206c76efd58370613bf7cc50447cad77d6ee1a17287ddd7a77ecc3340b02201e157c3068ed33a4115ac89e9a8fb0e989c1ff3017f2ecff261c58e73bc9fca1012102e1df9b80b04a6e641c7212da01dc0f16be35a20224587dfb82b1fcda87add8edffffffff040000000000000000126a104f41010003c0ecf1bb11d0b6a36d00008c0a00000000000017a914d9e4346a95219942e068363f749e53c7754ab2e987aa0a0000000000001976a914199236156e16d534f763349c02498255fb41758d88acb4f40100000000001976a91475eac11f3cfda3e79c90469dac0ebaaaacde701488ac00000000");
            var marker = tr.GetColoredMarker();
        }

        [TestMethod]
        public async Task SpentOutputs()
        {
            var outputService = Config.Services.GetService<IBitcoinOutputsService>();
            var repo = Config.Services.GetService<IInternalSpentOutputRepository>();

            var coins = await outputService.GetColoredUnspentOutputs("3Jr1pvk2uiA1W7TJwEmMVJQRLGdyfXrFKu",
                new BitcoinAssetId("AXkedGbAH1XGDpAypVzA5eyjegX4FaCnvM").AssetId);

            coins = coins.Take(coins.Count() / 2);
            foreach (var coloredCoin in coins)            
                await repo.Insert(coloredCoin.Outpoint.Hash.ToString(), (int)coloredCoin.Outpoint.N);            
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

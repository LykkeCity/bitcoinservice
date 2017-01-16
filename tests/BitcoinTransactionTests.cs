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
        }

        [Test]
        public async Task TestBroadcastCommitment()
        {
            var rpcClient = Config.Services.GetService<IRpcBitcoinClient>();
            var trService = Config.Services.GetService<IBitcoinTransactionService>();
            var helper = Config.Services.GetService<ITransactionBuildHelper>();

            var hex =
                "01000000022177aff6ea91735390f16d8e812e4016421b84685caeafd6d493c2e387b92826010000006a47304402207576124f5570e01482f10964dce977cc774e6779f82791b62fef1fa7a438c2ff022056734c906897ee3a523b19477db4aa7fa311c41db6ef4cb4437355c1747c4007012103e4407b9a8718b468aa59da0704ed9fd9e1f566e74f82d2c8f6a302127cda4fa3ffffffff9ef7933b504e50019e6f65088ae0dd402129ae11d126f715396fc9e0afc85307400000006a47304402202a3ed7b29a87f8286fb8df5b2bd0919e54b282270dc93f9c5eb1834de5b58a0f0220567c1f09563b1ba3c3c4ac014ab33355194b5f5b615ba135af35ed61b65b00b7012103e4407b9a8718b468aa59da0704ed9fd9e1f566e74f82d2c8f6a302127cda4fa3ffffffff040000000000000000106a0e4f41010002c096b102c0cc8d08008c0a00000000000017a9143a0737aa02c02ab43f2a502c850aff5f06712ea787aa0a0000000000001976a9146486f4670369e483b1cb982c216a9a0ba713237c88ac8ee40000000000001976a914ed75405f426601f5493117b5a22dc0082269e32288ac00000000";
            var tr = new Transaction(hex);
            
            await helper.AddFee(tr);
            
            await rpcClient.BroadcastTransaction(tr);
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
            var signProvider = Config.Services.GetService<ISignatureApiProvider>();
            var bitcoinClient = Config.Services.GetService<IRpcBitcoinClient>();
            var outputs = await bitcoinOutService.GetUncoloredUnspentOutputs(address);


            TransactionBuilder builder = new TransactionBuilder();            

            //helper.SendWithChange(builder, context, outputs.ToList(),
            //    CreateScript(pk.PubKey.ToHex(), pk2.PubKey.ToHex()).GetScriptAddress(Network.TestNet),
            //    new Money(0.1M, MoneyUnit.BTC), new BitcoinPubKeyAddress(address));
            //await helper.AddFee(builder, context);

            var tr = builder.BuildTransaction(true);

            var signed = await signProvider.SignTransaction(tr.ToHex());

            await bitcoinClient.BroadcastTransaction(new Transaction(signed));
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
            TransactionBuildContext context = new TransactionBuildContext(Network.TestNet, null);

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
                Pushes = new[] { new byte[0],  push1, push2 }
            };

            tr.Inputs[0].ScriptSig = OffchainScriptCommitmentTemplate.GenerateScriptSig(scriptParams);


            ScriptError error;
            tr.Inputs.AsIndexedInputs().First().VerifyScript(scriptCoin.ScriptPubKey, out error);
            await bitcoinClient.BroadcastTransaction(tr);

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

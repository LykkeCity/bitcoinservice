using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;
using Common;
using Core;
using Core.Bitcoin;
using Core.Exceptions;
using Core.Helpers;
using Core.OpenAssets;
using Core.Providers;
using Core.QBitNinja;
using Core.Repositories.Assets;
using Core.Repositories.MultipleCashouts;
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
        public async Task GetInfoTest()
        {
            var a = BitcoinAddress.Create("2NDAX5fg6Svo7TxtjoScQXy652kheU22u2K");

            var address1 = new PubKey("03676601a24b05f5652ef4ce3616f505541d42072b28057007f43e0874bb27e47d").GetAddress(Network.TestNet);
            var address2 = new PubKey("03ebcc2d675d17c5b5e250307cb0189bfc5adf6809bfd3c2823a2884dbbcaec58b").GetAddress(Network.TestNet);
            var address3 = new PubKey("04b1ce3afeb590c6a5295475e0eb91bc10feb9ccc3277d7f0e603059cd6a6d35a6eac01d3772af5251dafad36d5004ff839d5d4d49fa5386dd57a614188f4a84ee").GetAddress(Network.TestNet);

            var key = new Key();

            var pk = new BitcoinSecret("cRw7XsZ1Eo7XWjuZLkuUzkbMMHVSoLURjLpPgi179aWh42nmnPYh", Network.TestNet);
            //{0491590ab4eb4b5227b82dbdb8fbf5dc850325b953a2990036e33a5f538ff17acd1d345ce3199b55a4ebc111addb1b824c4aeafc97ecffed280f1cd0fd009dd8d7}
            var pubKey = pk.PubKey.ToString(Network.TestNet);
            var s = pk.PubKey.WitHash.ScriptPubKey.ToString();


            var script = CreateScript(new PubKey("02483c13420c9eec1846a6878fc0b41f481d4e9a66fc8038d48543a50053a71608"), new PubKey("0491590ab4eb4b5227b82dbdb8fbf5dc850325b953a2990036e33a5f538ff17acd1d345ce3199b55a4ebc111addb1b824c4aeafc97ecffed280f1cd0fd009dd8d7"), new PubKey("0491590ab4eb4b5227b82dbdb8fbf5dc850325b953a2990036e33a5f538ff17acd1d345ce3199b55a4ebc111addb1b824c4aeafc97ecffed280f1cd0fd009dd8d7"));

            var tr = new Transaction("0100000002b27d75295bbc11a7c99ccdf9ea871f7d8042afc8c8de941f537460363462e5056d01000000ffffffff3e5adda3f57aca38a3e555bcfeadfbe8e28c1eecfc17a028de4e1ba11c9d5ce0000000009300483045022100ccdf8728018753324a4f961d5e05a9b3a206ace65725f0c3d86ef68be91faf9b022014ad208c6bd5293a07f07e1c2004f59e1904a9f1f59a1902b2a3bd71deb7c92e0100475221039ae917fff146ea0196a5842cbf7d8c01b026463e6935481b8daff7a2fb5a0c1c2103f7358a6aa8f5f0ef36ca56be7e105701e30bfb46db8ee18cad379c91198106f052aeffffffff02df982c000000000017a91406a30d58e7d8fb28861b2781def3a6ef3c7763f387006c0100000000001976a91475eac11f3cfda3e79c90469dac0ebaaaacde701488ac00000000");
            var marker = tr.GetColoredMarker();
        }

        private class CashRequestEntity : ICashoutRequest
        {
            public Guid CashoutRequestId { get; }
            public decimal Amount { get; set; }
            public string DestinationAddress { get; set; }            
            public DateTime Date { get; }
            public Guid? MultipleCashoutId { get; }            
        }

        [TestMethod]
        public async Task Test()
        {

            var hotWallet = OpenAssetsHelper.ParseAddress("mj5FEqrC2P4FjFNfX8q3eZ4UABWUcRNy9r");
            var changeWallet = OpenAssetsHelper.ParseAddress("minog49vnNVuWK5kSs5Ut1iPyNZcR1i7he");

            var hotWalletOutputs = GenerateOutputs(5);

            var hotWalletBalance = new Money(hotWalletOutputs.Select(o => o.Amount).DefaultIfEmpty().Sum(o => o?.Satoshi ?? 0));

            var maxFeeForTransaction = Money.FromUnit(0.099M, MoneyUnit.BTC);

            var selectedCoins = new List<Coin>();

            var _feeProvider = Config.Services.GetService<IFeeProvider>();
            var _transactionBuildHelper = Config.Services.GetService<ITransactionBuildHelper>();
            var cashoutRequests = (GenerateCashoutRequest(200)).ToList();

            var maxInputsCount = maxFeeForTransaction.Satoshi / (await _feeProvider.GetFeeRate()).GetFee(Constants.InputSize).Satoshi;

            do
            {
                if (selectedCoins.Count > maxInputsCount && cashoutRequests.Count > 1)
                {
                    cashoutRequests = cashoutRequests.Take(cashoutRequests.Count - 1).ToList();
                    selectedCoins.Clear();
                }
                else
                    if (selectedCoins.Count > 0)
                        break;

                var totalAmount = Money.FromUnit(cashoutRequests.Select(o => o.Amount).Sum(), MoneyUnit.BTC);

                if (hotWalletBalance < totalAmount + maxFeeForTransaction)
                {
                    var changeBalance = Money.Zero;
                    List<Coin> changeWalletOutputs = new List<Coin>();
                    if (hotWallet != changeWallet)
                    {
                        changeWalletOutputs = GenerateOutputs(1).ToList();
                        changeBalance = new Money(changeWalletOutputs.Select(o => o.Amount).DefaultIfEmpty().Sum(o => o?.Satoshi ?? 0));
                    }
                    if (changeBalance + hotWalletBalance >= totalAmount + maxFeeForTransaction)
                    {
                        selectedCoins.AddRange(hotWalletOutputs);
                        selectedCoins.AddRange(OpenAssetsHelper
                            .CoinSelect(changeWalletOutputs, totalAmount + maxFeeForTransaction - hotWalletBalance).OfType<Coin>());
                    }
                    else
                    {
                        selectedCoins.AddRange(hotWalletOutputs);
                        selectedCoins.AddRange(changeWalletOutputs);

                        int cashoutsUsedCount = 0;
                        var cashoutsAmount = await _transactionBuildHelper.CalcFee(selectedCoins.Count, cashoutRequests.Count + 1);
                        foreach (var cashoutRequest in cashoutRequests)
                        {
                            cashoutsAmount += Money.FromUnit(cashoutRequest.Amount, MoneyUnit.BTC);
                            if (cashoutsAmount > hotWalletBalance + changeBalance)
                                break;
                            cashoutsUsedCount++;
                        }
                        if (cashoutsUsedCount == 0)
                            throw new BackendException("Not enough bitcoin available", ErrorCode.NotEnoughBitcoinAvailable);
                        cashoutRequests = cashoutRequests.Take(cashoutsUsedCount).ToList();
                    }
                }
                else
                {
                    selectedCoins.AddRange(OpenAssetsHelper.CoinSelect(hotWalletOutputs, totalAmount + maxFeeForTransaction).OfType<Coin>());
                }
            } while (true);

            var selectedCoinsAmount = new Money(selectedCoins.Sum(o => o.Amount));
            var sendAmount = new Money(cashoutRequests.Sum(o => o.Amount), MoneyUnit.BTC);
            var builder = new TransactionBuilder();

            builder.AddCoins(selectedCoins);
            foreach (var cashout in cashoutRequests)
            {
                var amount = Money.FromUnit(cashout.Amount, MoneyUnit.BTC);
                builder.Send(OpenAssetsHelper.ParseAddress(cashout.DestinationAddress), amount);
            }

            builder.Send(changeWallet, selectedCoinsAmount - sendAmount);

            builder.SubtractFees();
            builder.SendEstimatedFees(await _feeProvider.GetFeeRate());
            builder.SetChange(changeWallet);

            var tx = builder.BuildTransaction(true);
            _transactionBuildHelper.AggregateOutputs(tx);
        }

        private const int rndINit = 1;

        private IEnumerable<ICashoutRequest> GenerateCashoutRequest(int cnt)
        {
            var rand = new Random(rndINit);
            for (var i = 0; i < cnt; i++)
            {
                yield return new CashRequestEntity
                {
                    Amount = new Money(rand.Next(2700, 100000000)).ToDecimal(MoneyUnit.BTC),
                    DestinationAddress = new Key().PubKey.GetAddress(Network.TestNet).ToString()
                };
            }
        }


        private static int _N = 0;

        private IEnumerable<Coin> GenerateOutputs(int cnt)
        {
            var rand = new Random(rndINit);
            for (int i = 0; i < cnt; i++)
            {
                yield return new Coin(new OutPoint(uint256.One, _N++), new TxOut(rand.Next(2700, 500000000), OpenAssetsHelper.ParseAddress("mj5FEqrC2P4FjFNfX8q3eZ4UABWUcRNy9r")));
            }
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

            var helper = Config.Services.GetService<ITransactionBuildHelper>();

            var prevTx = new Transaction("0100000002347328a86bbd9d2a40e420e8d0a7da9986fd916b3ca02365c8d480936067a36cce0000008a47304402201eebb0365b67e534b72302987453d43833594965d7180c746dd5b1af27a7d6be02204496df6a47858fe9a3f6fd09dff90382e7c22a5a52c41300466bcfed808a1f39014104ff20028f41de7e2bac4f8e90884becad36c1390d2ab991a16fbcf745db478fd37cbf65c57a84e5a485bd5934c659f94aff35fe9fc50ebd2281ede40366190f57ffffffffab496631bf50d77e36303fb6156c3c73809c2023048e7e28b59c7272a8c509fc2f0000008b483045022100c6cb855117224ff9e7334ccf1030649c34a8e48eb7b6c7c4bf746d3ff67c1641022038257e6100c34c568f24d490a69b4591f93a56c433b6ad30102627a5a48ce0da01410496052ef8fb660bb338ba186dd2f52362c66b23f824295a6b74d0c60cf61a12e2b1f8b97512e09c20693f00dba9df3c644f245c120983d0582e4a88cb466ffa69ffffffff0300e1f5050000000017a914a9168848118a24ff8f848bac2eaaa248105b0307870084d717000000001976a91497a515ec03d9aada5e6f0d895f4aa10eb8f07e8d88ac800f0200000000001976a914ed75405f426601f5493117b5a22dc0082269e32288ac00000000");
            var coin = new Coin(prevTx, 0);

            var redeem = CreateScript(multisigFirstPartPk.PubKey, revokePk.PubKey, singlePk.PubKey);

            var addr = redeem.WitHash.ScriptPubKey.Hash.GetAddress(Network.TestNet);

            var scriptCoin = new ScriptCoin(coin, redeem);

            TransactionBuilder builder = new TransactionBuilder();
            TransactionBuildContext context = new TransactionBuildContext(Network.TestNet, null, null);

            //builder.AddKeys(pk);
            builder.AddCoins(scriptCoin);

            builder.Send(multisigFirstPartPk.PubKey.ScriptPubKey, "0.5");
            builder.SetChange(addr);
            builder.SendFees("0.001");

            var tr = builder.BuildTransaction(false);


            //tr.Inputs[0].Sequence = new Sequence(144);
            // tr.Version = 2;
            var hash = Script.SignatureHash(redeem, tr, 0, SigHash.All, scriptCoin.Amount, HashVersion.Witness);

            var signature = singlePk.PrivateKey.Sign(hash, SigHash.All).Signature.ToDER().Concat(new byte[] { 0x01 }).ToArray();

            var push1 = multisigFirstPartPk.PrivateKey.Sign(hash, SigHash.All).Signature.ToDER().Concat(new byte[] { 0x01 }).ToArray();
            var push2 = revokePk.PrivateKey.Sign(hash, SigHash.All).Signature.ToDER().Concat(new byte[] { 0x01 }).ToArray();

            var scriptParams = new OffchainScriptParams
            {
                IsMultisig = true,
                RedeemScript = redeem.ToBytes(),
                Pushes = new[] { new byte[0], new byte[0], new byte[0], }
            };

            tr.Inputs[0].WitScript = OffchainScriptCommitmentTemplate.GenerateScriptSig(scriptParams);
            tr.Inputs[0].ScriptSig = new Script(Op.GetPushOp(redeem.WitHash.ScriptPubKey.ToBytes(true)));

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

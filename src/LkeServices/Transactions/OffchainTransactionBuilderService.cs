using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Bitcoin;
using Core.Exceptions;
using Core.Helpers;
using Core.OpenAssets;
using Core.Repositories.Assets;
using LkeServices.Multisig;
using NBitcoin;
using NBitcoin.OpenAsset;
using Common;
using Core.Providers;
using Core.Repositories.Offchain;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Wallets;
using LkeServices.Providers;
using LkeServices.Signature;
using PhoneNumbers;

namespace LkeServices.Transactions
{
    public interface IOffchainTransactionBuilderService
    {
        Task<string> CreateUnsignedChannel(string clientPubKey, string hotWalletPubKey, decimal clientAmount, decimal hubAmount,
            string assetName);

        Task<string> CreateHubCommitment(string clientPubKey, string assetName, decimal clientAmount, decimal hubAmount,
            string signedByClientChannel);

        Task<string> FinalizeChannel(string clientPubKey, string hotWalletPubKey, string assetName, string clientRevokePubKey, string signedByClientHubCommitment);
    }

    public class OffchainTransactionBuilderService : IOffchainTransactionBuilderService
    {
        private const int OneDayDelay = 6 * 24; // 24 hours * 6 blocks in hour

        private readonly ITransactionBuildHelper _transactionBuildHelper;
        private readonly RpcConnectionParams _connectionParams;
        private readonly IMultisigService _multisigService;
        private readonly IBitcoinOutputsService _bitcoinOutputsService;
        private readonly IOffchainChannelRepository _offchainChannelRepository;
        private readonly ISignatureVerifier _signatureVerifier;
        private readonly ISignatureApiProvider _signatureApiProvider;
        private readonly ICommitmentRepository _commitmentRepository;
        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;
        private readonly ILykkeTransactionBuilderService _lykkeTransactionBuilderService;
        private readonly IAssetRepository _assetRepository;
        private readonly IBitcoinBroadcastService _broadcastService;

        public OffchainTransactionBuilderService(
            ITransactionBuildHelper transactionBuildHelper,
            RpcConnectionParams connectionParams,
            IMultisigService multisigService,
            IBitcoinOutputsService bitcoinOutputsService,
            IOffchainChannelRepository offchainChannelRepository,
            ISignatureVerifier signatureVerifier,
            Func<SignatureApiProviderType, ISignatureApiProvider> signatureApiProviderFactory,
            ICommitmentRepository commitmentRepository,
            IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory,
            IBroadcastedOutputRepository broadcastedOutputRepository,
            ILykkeTransactionBuilderService lykkeTransactionBuilderService,
            IAssetRepository assetRepository, IBitcoinBroadcastService broadcastService)
        {
            _transactionBuildHelper = transactionBuildHelper;
            _connectionParams = connectionParams;
            _multisigService = multisigService;
            _bitcoinOutputsService = bitcoinOutputsService;
            _offchainChannelRepository = offchainChannelRepository;
            _signatureVerifier = signatureVerifier;
            _signatureApiProvider = signatureApiProviderFactory(SignatureApiProviderType.Exchange);
            _commitmentRepository = commitmentRepository;
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _lykkeTransactionBuilderService = lykkeTransactionBuilderService;
            _assetRepository = assetRepository;
            _broadcastService = broadcastService;
        }

        public async Task<string> CreateUnsignedChannel(string clientPubKey, string hotWalletPubKey, decimal clientAmount, decimal hubAmount, string assetName)
        {
            var address = await _multisigService.GetMultisig(clientPubKey);

            if (address == null)
                throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

            var multisig = new BitcoinScriptAddress(address.MultisigAddress, _connectionParams.Network);

            var asset = await _assetRepository.GetAssetById(assetName);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);


            var clientAddress = new PubKey(clientPubKey).GetAddress(_connectionParams.Network);
            var hotWalletAddress = new PubKey(hotWalletPubKey).GetAddress(_connectionParams.Network);

            TransactionBuildContext context = new TransactionBuildContext(_connectionParams.Network, _pregeneratedOutputsQueueFactory);

            return await context.Build(async () =>
            {
                var builder = new TransactionBuilder();

                await SendToMultisig(clientAddress, multisig, asset, builder, context, clientAmount);
                await SendToMultisig(hotWalletAddress, multisig, asset, builder, context, hubAmount);
                await SendToMultisig(multisig, multisig, asset, builder, context, -1);

                await _transactionBuildHelper.AddFee(builder, context);
                var tr = builder.BuildTransaction(true);

                _transactionBuildHelper.AggregateOutputs(tr);

                var hex = tr.ToHex();
                var channel = await _offchainChannelRepository.CreateChannel(Guid.NewGuid(), multisig.ToWif(), assetName, hex);

                await _broadcastedOutputRepository.InsertOutputs(OpenAssetsHelper.OrderBasedColoringOutputs(tr, context)
                    .Select(o => new BroadcastedOutput(o, channel.TransactionId, _connectionParams.Network)));

                return hex;
            });
        }

        public async Task<string> CreateHubCommitment(string clientPubKey, string assetName, decimal clientAmount, decimal hubAmount,
            string signedByClientChannel)
        {
            var address = await _multisigService.GetMultisig(clientPubKey);

            if (address == null)
                throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

            var multisig = new BitcoinScriptAddress(address.MultisigAddress, _connectionParams.Network);

            var asset = await _assetRepository.GetAssetById(assetName);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);

            var channel = await _offchainChannelRepository.GetChannel(multisig.ToWif(), assetName);
            if (channel == null)
                throw new BackendException("Channel is not found", ErrorCode.ChannelNotFound);

            if (!TransactionComparer.CompareTransactions(channel.InitialTransaction, signedByClientChannel))
                throw new BackendException("Provided signed transaction is not equal initial transaction", ErrorCode.BadTransaction);

            if (!await _signatureVerifier.Verify(signedByClientChannel, clientPubKey))
                throw new BackendException("Provided signed transaction is not signed by client", ErrorCode.BadTransaction);

            var fullSignedChannel = await _signatureApiProvider.SignTransaction(signedByClientChannel);

            if (!_signatureVerifier.VerifyScriptSigs(fullSignedChannel))
                throw new BackendException("Channel transaction is not full signed", ErrorCode.BadFullSignTransaction);

            await _offchainChannelRepository.SetFullSignedTransactionAndAmount(multisig.ToWif(), assetName, fullSignedChannel, hubAmount, clientAmount);
            var hubRevokeKey = new Key();

            var tr = CreateCommitmentTransaction(address, new PubKey(address.ExchangePubKey), new PubKey(clientPubKey), hubRevokeKey.PubKey, new PubKey(clientPubKey), asset,
                hubAmount, clientAmount, fullSignedChannel);

            await _commitmentRepository.CreateCommitment(CommitmentType.Hub, multisig.ToWif(), assetName,
                                                         new BitcoinSecret(hubRevokeKey, _connectionParams.Network).ToString(), hubRevokeKey.PubKey.ToHex(), tr.ToHex());
            return tr.ToHex();
        }





        public async Task<string> FinalizeChannel(string clientPubKey, string hotWalletPubKey, string assetName, string clientRevokePubKey, string signedByClientHubCommitment)
        {
            var address = await _multisigService.GetMultisig(clientPubKey);

            if (address == null)
                throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

            var multisig = new BitcoinScriptAddress(address.MultisigAddress, _connectionParams.Network);

            var asset = await _assetRepository.GetAssetById(assetName);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);

            var channel = await _offchainChannelRepository.GetChannel(multisig.ToWif(), assetName);
            if (channel == null)
                throw new BackendException("Channel is not found", ErrorCode.ChannelNotFound);

            ICommitment commitment = await _commitmentRepository.GetLastCommitment(multisig.ToWif(), assetName, CommitmentType.Hub);
            if (commitment == null)
                throw new BackendException("Commitment is not found", ErrorCode.CommitmentNotFound);

            if (!TransactionComparer.CompareTransactions(signedByClientHubCommitment, commitment.InitialTransaction))
                throw new BackendException("Provided signed transaction is not equal initial transaction", ErrorCode.BadTransaction);

            if (!await _signatureVerifier.Verify(signedByClientHubCommitment, clientPubKey))
                throw new BackendException("Provided signed transaction is not signed by client", ErrorCode.BadTransaction);

            var fullSignedCommitment = await _signatureApiProvider.SignTransaction(signedByClientHubCommitment);

            if (!_signatureVerifier.VerifyScriptSigs(fullSignedCommitment))
                throw new BackendException("Transaction is not full signed", ErrorCode.BadFullSignTransaction);

            await _commitmentRepository.SetFullSignedTransaction(commitment.CommitmentId, multisig.ToWif(), assetName, fullSignedCommitment);

            var clientCommitment = CreateCommitmentTransaction(address, new PubKey(clientPubKey),
                new PubKey(hotWalletPubKey), new PubKey(clientRevokePubKey), new PubKey(address.ExchangePubKey), asset, channel.ClientAmount, channel.HubAmount, channel.FullySignedChannel);

            var signedByHubCommitment = await _signatureApiProvider.SignTransaction(clientCommitment.ToHex());

            await _commitmentRepository.CreateCommitment(CommitmentType.Client, multisig.ToWif(), assetName, null, clientRevokePubKey, signedByHubCommitment);

            var channelTr = new Transaction(channel.FullySignedChannel);

            await _broadcastService.BroadcastTransaction(channel.TransactionId, channelTr);

            await _lykkeTransactionBuilderService.SaveSpentOutputs(channel.TransactionId, channelTr);

            return signedByHubCommitment;
        }

        private async Task SendToMultisig(BitcoinAddress @from, BitcoinAddress toMultisig, IAsset assetEntity, TransactionBuilder builder, TransactionBuildContext context, decimal amount)
        {
            if (OpenAssetsHelper.IsBitcoin(assetEntity.Id))
            {
                Money sendAmount;
                var unspentOutputs = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(from.ToWif())).ToList();

                if (amount < 0)
                    sendAmount = unspentOutputs.OfType<Coin>().DefaultIfEmpty().Sum(o => o.Amount);
                else
                    sendAmount = Money.FromUnit(amount, MoneyUnit.BTC);

                if (sendAmount > 0)
                    _transactionBuildHelper.SendWithChange(builder, context, unspentOutputs, toMultisig, sendAmount, from);
            }
            else
            {
                var asset = new BitcoinAssetId(assetEntity.BlockChainAssetId, _connectionParams.Network).AssetId;
                long sendAmount;

                var unspentOutputs = (await _bitcoinOutputsService.GetColoredUnspentOutputs(from.ToWif(), asset)).ToList();
                if (amount < 0)
                    sendAmount = unspentOutputs.OfType<ColoredCoin>().Sum(o => o.Amount.Quantity);
                else
                    sendAmount = new AssetMoney(asset, amount, assetEntity.MultiplierPower).Quantity;
                if (sendAmount > 0)
                    _transactionBuildHelper.SendAssetWithChange(builder, context, unspentOutputs,
                        toMultisig, new AssetMoney(asset, sendAmount), @from);
            }
        }

        private Script CreateOffchainScript(PubKey pubKey1, PubKey pubKey2, PubKey lockedPubKey)
        {
            var multisigScriptOps = PayToMultiSigTemplate.Instance.GenerateScriptPubKey
               (2, pubKey1, pubKey2).ToOps();
            var ops = new List<Op>();

            ops.Add(OpcodeType.OP_IF);
            ops.AddRange(multisigScriptOps);
            ops.Add(OpcodeType.OP_ELSE);
            ops.Add(Op.GetPushOp(OneDayDelay));
            ops.Add(OpcodeType.OP_CHECKSEQUENCEVERIFY);
            ops.Add(OpcodeType.OP_DROP);
            ops.Add(Op.GetPushOp(lockedPubKey.ToBytes()));
            ops.Add(OpcodeType.OP_CHECKSIG);
            ops.Add(OpcodeType.OP_ENDIF);

            return new Script(ops.ToArray());
        }

        private ICoin FindCoin(Transaction tr, string multisig, string walletRedeemScript, decimal amount, IAsset asset)
        {
            if (OpenAssetsHelper.IsBitcoin(asset.Id))
            {
                var money = new Money(amount, MoneyUnit.BTC);
                return tr.Outputs.AsCoins().FirstOrDefault(o => o.Amount == money &&
                        o.ScriptPubKey.GetDestinationAddress(_connectionParams.Network).ToWif() == multisig);
            }
            var assetMoney = new AssetMoney(new BitcoinAssetId(asset.BlockChainAssetId), amount, asset.MultiplierPower);
            uint markerPosition;
            var marker = ColorMarker.Get(tr, out markerPosition);
            var found = tr.Outputs.AsIndexedOutputs()
                .FirstOrDefault(o => o.TxOut.ScriptPubKey.GetDestinationAddress(_connectionParams.Network)?.ToWif() == multisig &&
                                     o.N > markerPosition && marker.Quantities[o.N - markerPosition - 1] == (ulong)assetMoney.Quantity);
            return found?.ToCoin().ToScriptCoin(new Script(walletRedeemScript)).ToColoredCoin(assetMoney);
        }


        private Transaction CreateCommitmentTransaction(IWalletAddress wallet, PubKey lockedPubKey, PubKey unlockedPubKey, PubKey revokePubKey, PubKey multisigPairPubKey,
            IAsset asset, decimal lockedAmount, decimal unlockedAmount, string channelTr)
        {
            var multisig = new BitcoinScriptAddress(wallet.MultisigAddress, _connectionParams.Network);
            var channel = new Transaction(channelTr);
            var spendCoin = FindCoin(channel, multisig.ToWif(), wallet.RedeemScript, lockedAmount + unlockedAmount, asset);

            if (spendCoin == null)
                throw new BackendException($"Not found output in setup channel with amount {lockedAmount + unlockedAmount}", ErrorCode.NoCoinsFound);

            TransactionBuilder builder = new TransactionBuilder();
            builder.AddCoins(spendCoin);
            long additionalBtc = 0;
            var script = CreateOffchainScript(multisigPairPubKey, revokePubKey, lockedPubKey);

            var unlockedAddress = unlockedPubKey.GetAddress(_connectionParams.Network);
            var lockedAddress = script.GetScriptAddress(_connectionParams.Network);

            if (OpenAssetsHelper.IsBitcoin(asset.Id))
            {
                if (unlockedAmount > 0)
                    builder.Send(unlockedAddress, new Money(unlockedAmount, MoneyUnit.BTC));
                if (lockedAmount > 0)
                    builder.Send(lockedAddress, new Money(lockedAmount, MoneyUnit.BTC));
            }
            else
            {
                var sendAmount = ((ColoredCoin)spendCoin).Bearer.Amount;
                var dustAmount = 0L;
                var assetId = new BitcoinAssetId(asset.BlockChainAssetId).AssetId;
                if (unlockedAmount > 0)
                {
                    builder.SendAsset(unlockedAddress, new AssetMoney(assetId, unlockedAmount, asset.MultiplierPower));
                    dustAmount += new TxOut(Money.Zero, unlockedAddress.ScriptPubKey).GetDustThreshold(builder.StandardTransactionPolicy.MinRelayTxFee);
                }
                if (lockedAmount > 0)
                {
                    builder.Send(lockedAddress, new AssetMoney(assetId, lockedAmount, asset.MultiplierPower));
                    dustAmount += new TxOut(Money.Zero, lockedAddress.ScriptPubKey).GetDustThreshold(builder.StandardTransactionPolicy.MinRelayTxFee);
                }
                additionalBtc = dustAmount - sendAmount;
            }

            var fakeFee = new Money(1, MoneyUnit.BTC);

            var fakeAmount = additionalBtc + fakeFee;
            builder.SendFees(fakeFee);

            _transactionBuildHelper.AddFakeInput(builder, fakeAmount);
            var tr = builder.BuildTransaction(true);
            _transactionBuildHelper.RemoveFakeInput(tr);
            return tr;
        }
    }
}

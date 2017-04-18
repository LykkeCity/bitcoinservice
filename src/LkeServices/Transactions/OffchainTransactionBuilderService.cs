using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
using Core.Repositories.RevokeKeys;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Wallets;
using Core.ScriptTemplates;
using LkeServices.Providers;
using LkeServices.Signature;

namespace LkeServices.Transactions
{

    public class OffchainResponse
    {
        public Guid TransferId { get; set; }

        public string TransactionHex { get; set; }
    }

    public interface IOffchainTransactionBuilderService
    {
        Task<OffchainResponse> CreateTransfer(string clientPubKey, decimal amount, IAsset asset, string clientPrevPrivateKey, bool requiredTransfer, Guid? transferId);

        Task<OffchainResponse> CreateUnsignedChannel(string clientPubKey, string hotWalletPubKey, decimal hubAmount, IAsset asset, bool requiredTransfer, Guid? transferId);

        Task<OffchainResponse> CreateCashin(string clientPubKey, decimal amount, IAsset asset, string cashinAddress, Guid? transferId);

        Task<OffchainResponse> CreateHubCommitment(string clientPubKey, IAsset asset, decimal amount, string signedByClientChannel);

        Task<OffchainResponse> Finalize(string clientPubKey, string hotWalletPubKey, IAsset asset, string clientRevokePubKey, string signedByClientHubCommitment);

        Task<OffchainResponse> CloseChannel(string clientPubKey, string cashoutAddress, string hotWalletPubKey, IAsset asset);

        Task<string> BroadcastClosingChannel(string clientPubKey, IAsset asset, string signedByClientTransaction);

        Task SpendCommitmemtByMultisig(ICommitment commitment, ICoin spendingCoin, string destination);

        Task<string> BroadcastCommitment(string clientPubKey, IAsset asset, string transaction);

        Task CloseChannel(ICommitment commitment);

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
        private readonly IBroadcastedOutputRepository _broadcastedOutputRepository;
        private readonly IRevokeKeyRepository _revokeKeyRepository;
        private readonly ILykkeTransactionBuilderService _lykkeTransactionBuilderService;
        private readonly IOffchainTransferRepository _offchainTransferRepository;
        private readonly TransactionBuildContextFactory _transactionBuildContextFactory;
        private readonly IBitcoinBroadcastService _broadcastService;
        private readonly IClosingChannelRepository _closingChannelRepository;

        public OffchainTransactionBuilderService(
            ITransactionBuildHelper transactionBuildHelper,
            RpcConnectionParams connectionParams,
            IMultisigService multisigService,
            IBitcoinOutputsService bitcoinOutputsService,
            IOffchainChannelRepository offchainChannelRepository,
            ISignatureVerifier signatureVerifier,
            Func<SignatureApiProviderType, ISignatureApiProvider> signatureApiProviderFactory,
            ICommitmentRepository commitmentRepository,
            IBroadcastedOutputRepository broadcastedOutputRepository,
            IRevokeKeyRepository revokeKeyRepository,
            ILykkeTransactionBuilderService lykkeTransactionBuilderService,
            IOffchainTransferRepository offchainTransferRepository,
            TransactionBuildContextFactory transactionBuildContextFactory,
            IBitcoinBroadcastService broadcastService,
            IClosingChannelRepository closingChannelRepository)
        {
            _transactionBuildHelper = transactionBuildHelper;
            _connectionParams = connectionParams;
            _multisigService = multisigService;
            _bitcoinOutputsService = bitcoinOutputsService;
            _offchainChannelRepository = offchainChannelRepository;
            _signatureVerifier = signatureVerifier;
            _signatureApiProvider = signatureApiProviderFactory(SignatureApiProviderType.Exchange);
            _commitmentRepository = commitmentRepository;
            _broadcastedOutputRepository = broadcastedOutputRepository;
            _revokeKeyRepository = revokeKeyRepository;
            _lykkeTransactionBuilderService = lykkeTransactionBuilderService;
            _offchainTransferRepository = offchainTransferRepository;
            _transactionBuildContextFactory = transactionBuildContextFactory;
            _broadcastService = broadcastService;
            _closingChannelRepository = closingChannelRepository;
        }

        private async Task CheckTransferFinalization(string multisig, string assetId, Guid? transferId, bool throwOpenNewChannelException)
        {
            var transfer = await _offchainTransferRepository.GetLastTransfer(multisig, assetId);
            if (transfer == null)
                return;
            if (transfer.Completed)
                return;
            if (!transfer.Completed && transfer.Required && transfer.TransferId != transferId)
                throw new BackendException("Channel is not finalized", ErrorCode.ChannelNotFinalized);

            await _offchainTransferRepository.CloseTransfer(multisig, assetId, transfer.TransferId);

            var channel = await _offchainChannelRepository.GetChannel(multisig, assetId);
            if (channel != null && !channel.IsBroadcasted)
            {
                await _offchainChannelRepository.RevertChannel(multisig, assetId, channel.ChannelId);
                await _commitmentRepository.RemoveCommitmentsOfChannel(multisig, assetId, channel.ChannelId);
                await _lykkeTransactionBuilderService.RemoveSpenOutputs(new Transaction(channel.InitialTransaction));
            }
            if (throwOpenNewChannelException)
                throw new BackendException("Should open new channel", ErrorCode.ShouldOpenNewChannel);
        }

        public async Task<OffchainResponse> CreateTransfer(string clientPubKey, decimal amount, IAsset asset, string clientPrevPrivateKey, bool requiredTransfer, Guid? transferId)
        {
            var address = await _multisigService.GetMultisig(clientPubKey);

            if (address == null)
                throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

            await CheckTransferFinalization(address.MultisigAddress, asset.Id, transferId, true);

            var channel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);
            if (channel == null)
                throw new BackendException("Channel is not found", ErrorCode.ShouldOpenNewChannel);

            if (amount < 0 && channel.ClientAmount < Math.Abs(amount))
                throw new BackendException("Client amount in channel is low than required", ErrorCode.ShouldOpenNewChannel);

            if (amount > 0 && channel.HubAmount < amount)
                throw new BackendException("Hub amount in channel is low than required", ErrorCode.ShouldOpenNewChannel);

            if (string.IsNullOrWhiteSpace(clientPrevPrivateKey))
                throw new BackendException("Private key for previous commitment is required", ErrorCode.ShouldOpenNewChannel);

            var prevCommitment = await _commitmentRepository.GetLastCommitment(address.MultisigAddress, asset.Id, CommitmentType.Client);

            var secret = new BitcoinSecret(clientPrevPrivateKey);

            if (prevCommitment.RevokePubKey != secret.PubKey.ToHex())
                throw new BackendException("Client private key for previous commitment is invalid", ErrorCode.PrivateKeyIsBad);

            var closing = await _closingChannelRepository.GetClosingChannel(address.MultisigAddress, asset.Id);
            if (closing != null)
                await _closingChannelRepository.CompleteClosingChannel(address.MultisigAddress, asset.Id, closing.ClosingChannelId);

            await _revokeKeyRepository.AddPrivateKey(prevCommitment.RevokePubKey, clientPrevPrivateKey);

            var hubRevokeKey = new Key();


            var newHubAmount = channel.HubAmount - amount;
            var newClientAmount = channel.ClientAmount + amount;

            var commitmentResult = CreateCommitmentTransaction(address, new PubKey(address.ExchangePubKey), new PubKey(clientPubKey), hubRevokeKey.PubKey, new PubKey(clientPubKey), asset,
                newHubAmount, newClientAmount, channel.FullySignedChannel);

            var transfer = await _offchainTransferRepository.CreateTransfer(address.MultisigAddress, asset.Id, requiredTransfer);

            await _commitmentRepository.CreateCommitment(CommitmentType.Hub, channel.ChannelId, address.MultisigAddress, asset.Id,
                                                         hubRevokeKey.PubKey.ToHex(), commitmentResult.Transaction.ToHex(), newClientAmount,
                                                         newHubAmount, commitmentResult.LockedAddress, commitmentResult.LockedScript);
            await _revokeKeyRepository.AddRevokeKey(hubRevokeKey.PubKey.ToHex(), RevokeKeyType.Exchange, hubRevokeKey.ToString(_connectionParams.Network));

            return new OffchainResponse
            {
                TransactionHex = commitmentResult.Transaction.ToHex(),
                TransferId = transfer.TransferId
            };
        }

        public async Task<OffchainResponse> CreateUnsignedChannel(string clientPubKey, string hotWalletPubKey, decimal hubAmount, IAsset asset, bool requiredTransfer, Guid? transferId)
        {
            var address = await _multisigService.GetMultisig(clientPubKey);

            if (address == null)
                throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

            var multisig = new BitcoinScriptAddress(address.MultisigAddress, _connectionParams.Network);

            var hotWalletAddress = new PubKey(hotWalletPubKey).GetAddress(_connectionParams.Network);

            await CheckTransferFinalization(address.MultisigAddress, asset.Id, transferId, false);

            var currentChannel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);

            if (currentChannel != null && !currentChannel.IsBroadcasted)
                throw new BackendException("There is another pending channel setup", ErrorCode.AnotherChannelSetupExists);

            var context = _transactionBuildContextFactory.Create(_connectionParams.Network);
            return await context.Build(async () =>
            {
                var builder = new TransactionBuilder();

                var multisigAmount = await SendToMultisig(multisig, multisig, asset, builder, context, -1);
                decimal clientChannelAmount, hubChannelAmount;
                if (currentChannel == null)
                {
                    clientChannelAmount = multisigAmount;
                    hubChannelAmount = hubAmount;

                    await SendToMultisig(hotWalletAddress, multisig, asset, builder, context, hubAmount);                    
                }
                else
                {
                    clientChannelAmount = currentChannel.ClientAmount;
                    hubChannelAmount = Math.Max(0, hubAmount - currentChannel.HubAmount);

                    await SendToMultisig(hotWalletAddress, multisig, asset, builder, context, hubChannelAmount);

                    hubChannelAmount += currentChannel.HubAmount;
                }

                await _transactionBuildHelper.AddFee(builder, context);
                var tr = builder.BuildTransaction(true);

                _transactionBuildHelper.AggregateOutputs(tr);

                var hex = tr.ToHex();

                var transfer = await _offchainTransferRepository.CreateTransfer(multisig.ToWif(), asset.Id, requiredTransfer);
                var channel = await _offchainChannelRepository.CreateChannel(multisig.ToWif(), asset.Id, hex, clientChannelAmount, hubChannelAmount);

                await _lykkeTransactionBuilderService.SaveSpentOutputs(channel.ChannelId, tr);

                await _broadcastedOutputRepository.InsertOutputs(OpenAssetsHelper.OrderBasedColoringOutputs(tr, context)
                    .Select(o => new BroadcastedOutput(o, channel.ChannelId, _connectionParams.Network)));

                return new OffchainResponse
                {
                    TransactionHex = hex,
                    TransferId = transfer.TransferId
                };
            });
        }

        public async Task<OffchainResponse> CreateCashin(string clientPubKey, decimal amount, IAsset asset, string cashinAddr, Guid? transferId)
        {
            var address = await _multisigService.GetMultisig(clientPubKey);

            if (address == null)
                throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

            var multisig = new BitcoinScriptAddress(address.MultisigAddress, _connectionParams.Network);

            var cashinAddress = BitcoinAddress.Create(cashinAddr, _connectionParams.Network);

            await CheckTransferFinalization(address.MultisigAddress, asset.Id, transferId, false);

            var currentChannel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);

            if (currentChannel != null && !currentChannel.IsBroadcasted)
                throw new BackendException("There is another pending channel setup", ErrorCode.AnotherChannelSetupExists);

            var context = _transactionBuildContextFactory.Create(_connectionParams.Network);
            return await context.Build(async () =>
            {
                var builder = new TransactionBuilder();

                var multisigAmount = await SendToMultisig(multisig, multisig, asset, builder, context, -1);
                decimal clientChannelAmount, hubChannelAmount = 0;
                if (currentChannel == null)
                {
                    clientChannelAmount = amount + multisigAmount;
                    await SendToMultisig(cashinAddress, multisig, asset, builder, context, amount);
                }
                else
                {
                    clientChannelAmount = amount + currentChannel.ClientAmount;
                    hubChannelAmount = currentChannel.HubAmount;
                    await SendToMultisig(cashinAddress, multisig, asset, builder, context, amount);
                }

                await _transactionBuildHelper.AddFee(builder, context);
                var tr = builder.BuildTransaction(true);

                _transactionBuildHelper.AggregateOutputs(tr);

                var hex = tr.ToHex();

                var transfer = await _offchainTransferRepository.CreateTransfer(multisig.ToWif(), asset.Id, false);
                var channel = await _offchainChannelRepository.CreateChannel(multisig.ToWif(), asset.Id, hex, clientChannelAmount, hubChannelAmount);

                await _lykkeTransactionBuilderService.SaveSpentOutputs(channel.ChannelId, tr);

                await _broadcastedOutputRepository.InsertOutputs(OpenAssetsHelper.OrderBasedColoringOutputs(tr, context)
                    .Select(o => new BroadcastedOutput(o, channel.ChannelId, _connectionParams.Network)));

                return new OffchainResponse
                {
                    TransactionHex = hex,
                    TransferId = transfer.TransferId
                };
            });
        }

        public async Task<OffchainResponse> CreateHubCommitment(string clientPubKey, IAsset asset, decimal amount, string signedByClientChannel)
        {
            var address = await _multisigService.GetMultisig(clientPubKey);

            if (address == null)
                throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

            var channel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);
            if (channel == null)
                throw new BackendException("Channel is not found", ErrorCode.ShouldOpenNewChannel);

            if (amount < 0 && channel.ClientAmount < Math.Abs(amount))
                throw new BackendException("Client amount in channel is low than required", ErrorCode.BadChannelAmount);

            if (amount > 0 && channel.HubAmount < amount)
                throw new BackendException("Hub amount in channel is low than required", ErrorCode.BadChannelAmount);

            if (!TransactionComparer.CompareTransactions(channel.InitialTransaction, signedByClientChannel))
                throw new BackendException("Provided signed transaction is not equal initial transaction", ErrorCode.BadTransaction);

            if (!await _signatureVerifier.Verify(signedByClientChannel, clientPubKey))
                throw new BackendException("Provided signed transaction is not signed by client", ErrorCode.BadTransaction);

            var fullSignedChannel = await _signatureApiProvider.SignTransaction(signedByClientChannel);

            if (!_signatureVerifier.VerifyScriptSigs(fullSignedChannel))
                throw new BackendException("Channel transaction is not full signed", ErrorCode.BadFullSignTransaction);

            await _offchainChannelRepository.SetFullSignedTransaction(address.MultisigAddress, asset.Id, fullSignedChannel);

            var hubRevokeKey = new Key();

            var newHubAmount = channel.HubAmount - amount;
            var newClientAmount = channel.ClientAmount + amount;
            var commitmentResult = CreateCommitmentTransaction(address, new PubKey(address.ExchangePubKey), new PubKey(clientPubKey), hubRevokeKey.PubKey, new PubKey(clientPubKey), asset,
                newHubAmount, newClientAmount, fullSignedChannel);

            await _commitmentRepository.CreateCommitment(CommitmentType.Hub, channel.ChannelId, address.MultisigAddress, asset.Id,
                                                         hubRevokeKey.PubKey.ToHex(), commitmentResult.Transaction.ToHex(), newClientAmount,
                                                         newHubAmount, commitmentResult.LockedAddress, commitmentResult.LockedScript);
            await _revokeKeyRepository.AddRevokeKey(hubRevokeKey.PubKey.ToHex(), RevokeKeyType.Exchange, hubRevokeKey.ToString(_connectionParams.Network));

            var transfer = await _offchainTransferRepository.GetLastTransfer(address.MultisigAddress, asset.Id);

            return new OffchainResponse
            {
                TransactionHex = commitmentResult.Transaction.ToHex(),
                TransferId = transfer.TransferId
            };
        }

        public async Task<OffchainResponse> Finalize(string clientPubKey, string hotWalletPubKey, IAsset asset, string clientRevokePubKey, string signedByClientHubCommitment)
        {
            var address = await _multisigService.GetMultisig(clientPubKey);

            if (address == null)
                throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

            var channel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);
            if (channel == null)
                throw new BackendException("Channel is not found", ErrorCode.ShouldOpenNewChannel);

            var hubCommitment = await _commitmentRepository.GetLastCommitment(address.MultisigAddress, asset.Id, CommitmentType.Hub);
            if (hubCommitment == null)
                throw new BackendException("Commitment is not found", ErrorCode.CommitmentNotFound);

            if (await _revokeKeyRepository.GetRevokeKey(clientRevokePubKey) != null)
                throw new BackendException("Client revoke public key was used already", ErrorCode.KeyUsedAlready);


            if (!TransactionComparer.CompareTransactions(signedByClientHubCommitment, hubCommitment.InitialTransaction))
                throw new BackendException("Provided signed transaction is not equal initial transaction", ErrorCode.BadTransaction);

            if (!await _signatureVerifier.Verify(signedByClientHubCommitment, clientPubKey, SigHash.Single | SigHash.AnyoneCanPay))
                throw new BackendException("Provided signed transaction is not signed by client", ErrorCode.BadTransaction);

            var fullSignedCommitment = await _signatureApiProvider.SignTransaction(signedByClientHubCommitment, SigHash.Single | SigHash.AnyoneCanPay);

            if (!_signatureVerifier.VerifyScriptSigs(fullSignedCommitment))
                throw new BackendException("Transaction is not full signed", ErrorCode.BadFullSignTransaction);

            await _commitmentRepository.SetFullSignedTransaction(hubCommitment.CommitmentId, address.MultisigAddress, asset.Id, fullSignedCommitment);

            var clientCommitmentResult = CreateCommitmentTransaction(address, new PubKey(clientPubKey),
                new PubKey(hotWalletPubKey), new PubKey(clientRevokePubKey), new PubKey(address.ExchangePubKey), asset,
               hubCommitment.ClientAmount, hubCommitment.HubAmount, channel.FullySignedChannel);

            var signedByHubCommitment = await _signatureApiProvider.SignTransaction(clientCommitmentResult.Transaction.ToHex(), SigHash.Single | SigHash.AnyoneCanPay);

            await _commitmentRepository.CreateCommitment(CommitmentType.Client, channel.ChannelId, address.MultisigAddress, asset.Id,
                                                            clientRevokePubKey, signedByHubCommitment, hubCommitment.ClientAmount, hubCommitment.HubAmount,
                                                            clientCommitmentResult.LockedAddress, clientCommitmentResult.LockedScript);
            await _revokeKeyRepository.AddRevokeKey(clientRevokePubKey, RevokeKeyType.Client);

            if (!channel.IsBroadcasted)
            {
                var channelTr = new Transaction(channel.FullySignedChannel);

                await _broadcastService.BroadcastTransaction(channel.ChannelId, channelTr);

                await _offchainChannelRepository.SetChannelBroadcasted(address.MultisigAddress, asset.Id);

                if (channel.PrevChannelTransactionId.HasValue)
                    await _commitmentRepository.CloseCommitmentsOfChannel(address.MultisigAddress, asset.Id, channel.PrevChannelTransactionId.Value);
            }

            await _offchainChannelRepository.UpdateAmounts(address.MultisigAddress, asset.Id, hubCommitment.ClientAmount, hubCommitment.HubAmount);
            var transfer = await _offchainTransferRepository.GetLastTransfer(address.MultisigAddress, asset.Id);
            await _offchainTransferRepository.CompleteTransfer(address.MultisigAddress, asset.Id, transfer.TransferId);
            return new OffchainResponse
            {
                TransactionHex = signedByHubCommitment,
                TransferId = transfer.TransferId
            };
        }

        public async Task<OffchainResponse> CloseChannel(string clientPubKey, string cashoutAddr, string hotWalletPubKey, IAsset asset)
        {
            var address = await _multisigService.GetMultisig(clientPubKey);

            if (address == null)
                throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

            var multisig = new BitcoinScriptAddress(address.MultisigAddress, _connectionParams.Network);

            var cashoutAddress = new BitcoinScriptAddress(cashoutAddr, _connectionParams.Network);
            var hotWalletAddress = new PubKey(hotWalletPubKey).GetAddress(_connectionParams.Network);

            await CheckTransferFinalization(address.MultisigAddress, asset.Id, null, false);

            var channel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);

            if (channel == null)
                throw new BackendException("Channel is not found", ErrorCode.ShouldOpenNewChannel);

            if (!channel.IsBroadcasted)
                throw new BackendException("There is another pending channel setup", ErrorCode.AnotherChannelSetupExists);

            var context = _transactionBuildContextFactory.Create(_connectionParams.Network);

            var currentClosing = await _closingChannelRepository.GetClosingChannel(address.MultisigAddress, asset.Id);
            if (currentClosing != null)
                await _closingChannelRepository.CompleteClosingChannel(address.MultisigAddress, asset.Id, currentClosing.ClosingChannelId);

            return await context.Build(async () =>
            {
                var builder = new TransactionBuilder();

                var coin = FindCoin(new Transaction(channel.FullySignedChannel), multisig.ToWif(), address.RedeemScript,
                    channel.ClientAmount + channel.HubAmount, asset);

                builder.AddCoins(coin);

                if (OpenAssetsHelper.IsBitcoin(asset.Id))
                {
                    if (channel.ClientAmount > 0)
                        builder.Send(cashoutAddress, Money.FromUnit(channel.ClientAmount, MoneyUnit.BTC));
                    if (channel.HubAmount > 0)
                        builder.Send(hotWalletAddress, Money.FromUnit(channel.HubAmount, MoneyUnit.BTC));
                }
                else
                {
                    if (channel.ClientAmount > 0)
                        builder.SendAsset(cashoutAddress,
                            new AssetMoney(new BitcoinAssetId(asset.BlockChainAssetId).AssetId, channel.ClientAmount, asset.MultiplierPower));
                    if (channel.HubAmount > 0)
                        builder.SendAsset(hotWalletAddress,
                            new AssetMoney(new BitcoinAssetId(asset.BlockChainAssetId).AssetId, channel.HubAmount, asset.MultiplierPower));
                }

                await _transactionBuildHelper.AddFee(builder, context);

                var tr = builder.BuildTransaction(true);

                var hex = tr.ToHex();
                var closing = await _closingChannelRepository.CreateClosingChannel(address.MultisigAddress, asset.Id, channel.ChannelId, hex);

                await _broadcastedOutputRepository.InsertOutputs(OpenAssetsHelper.OrderBasedColoringOutputs(tr, context)
                    .Select(o => new BroadcastedOutput(o, closing.ClosingChannelId, _connectionParams.Network)));

                return new OffchainResponse
                {
                    TransactionHex = hex,
                    TransferId = closing.ClosingChannelId
                };
            });

        }

        public async Task<string> BroadcastClosingChannel(string clientPubKey, IAsset asset, string signedByClientTransaction)
        {
            var address = await _multisigService.GetMultisig(clientPubKey);

            if (address == null)
                throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);
            
            await CheckTransferFinalization(address.MultisigAddress, asset.Id, null, false);

            var channel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);

            if (channel == null)
                throw new BackendException("Channel is not found", ErrorCode.ShouldOpenNewChannel);

            if (!channel.IsBroadcasted)
                throw new BackendException("There is another pending channel setup", ErrorCode.AnotherChannelSetupExists);

            var closing = await _closingChannelRepository.GetClosingChannel(address.MultisigAddress, asset.Id);

            if (closing == null)
                throw new BackendException("Closing channel is not found", ErrorCode.ClosingChannelNotFound);

            if (closing.ChannelId != channel.ChannelId)
                throw new BackendException("Closing channel belong to expired channel", ErrorCode.ClosingChannelExpired);

            if (!TransactionComparer.CompareTransactions(closing.InitialTransaction, signedByClientTransaction))
                throw new BackendException("Provided signed transaction is not equal initial transaction", ErrorCode.BadTransaction);

            var fullSigned = await _signatureApiProvider.SignTransaction(signedByClientTransaction);

            var tr = new Transaction(fullSigned);

            await _broadcastService.BroadcastTransaction(closing.ClosingChannelId, tr);

            await _offchainChannelRepository.CloseChannel(address.MultisigAddress, asset.Id, channel.ChannelId);

            await _commitmentRepository.CloseCommitmentsOfChannel(address.MultisigAddress, asset.Id, channel.ChannelId);

            await _closingChannelRepository.CompleteClosingChannel(address.MultisigAddress, asset.Id, closing.ClosingChannelId);

            await _lykkeTransactionBuilderService.SaveSpentOutputs(closing.ClosingChannelId, tr);

            return tr.GetHash().ToString();
        }

        public async Task SpendCommitmemtByMultisig(ICommitment commitment, ICoin spendingCoin, string destination)
        {

            TransactionBuildContext context = _transactionBuildContextFactory.Create(_connectionParams.Network);

            var destinationAddress = BitcoinAddress.Create(destination);

            await context.Build(async () =>
                {
                    TransactionBuilder builder = new TransactionBuilder();
                    builder.AddCoins(spendingCoin);
                    if (OpenAssetsHelper.IsBitcoin(commitment.AssetId))
                        builder.Send(destinationAddress, spendingCoin.Amount);
                    else
                        builder.SendAsset(destinationAddress, ((ColoredCoin)spendingCoin).Amount);
                    await _transactionBuildHelper.AddFee(builder, context);

                    var tr = builder.BuildTransaction(false);

                    var redeem = commitment.LockedScript.ToScript();

                    var revokePubKey = OffchainScriptCommitmentTemplate.ExtractScriptPubKeyParameters(redeem).MultisigPubKeys[1];

                    var privateRevokeKey = (await _revokeKeyRepository.GetRevokeKey(revokePubKey.ToHex())).PrivateKey;

                    var scriptParams = new OffchainScriptParams
                    {
                        IsMultisig = true,
                        RedeemScript = redeem.ToBytes(),
                        Pushes = new[] { new byte[0], new byte[0], new byte[0] }
                    };

                    tr.Inputs[0].ScriptSig = OffchainScriptCommitmentTemplate.GenerateScriptSig(scriptParams);

                    var signed = await _signatureApiProvider.SignTransaction(tr.ToHex(), additionalSecrets: new[] { privateRevokeKey });

                    var signedTr = new Transaction(signed);
                    var id = Guid.NewGuid();
                    await _broadcastService.BroadcastTransaction(id, signedTr);

                    await _lykkeTransactionBuilderService.SaveSpentOutputs(id, signedTr);

                    return Task.CompletedTask;
                });
        }

        public async Task<string> BroadcastCommitment(string clientPubKey, IAsset asset, string transactionHex)
        {
            var address = await _multisigService.GetMultisig(clientPubKey);

            if (address == null)
                throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

            var channel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);
            if (channel == null)
                throw new BackendException("Channel is not found", ErrorCode.ShouldOpenNewChannel);
            if (!channel.IsBroadcasted)
                throw new BackendException("Channel is not finalized", ErrorCode.ChannelNotFinalized);

            var commitment = await _commitmentRepository.GetCommitment(address.MultisigAddress, asset.Id, transactionHex);
            if (commitment == null)
                throw new BackendException("Commitment is not found", ErrorCode.CommitmentNotFound);
            if (commitment.ChannelId != channel.ChannelId)
                throw new BackendException("Commitment is expired", ErrorCode.CommitmentExpired);

            var lastCommitment = await _commitmentRepository.GetLastCommitment(address.MultisigAddress, asset.Id, commitment.Type);

            //if (commitment.CommitmentId != lastCommitment.CommitmentId)
            //    throw new BackendException("Commitment is expired", ErrorCode.CommitmentExpired);

            TransactionBuildContext context = _transactionBuildContextFactory.Create(_connectionParams.Network);

            return await context.Build(async () =>
            {
                var transaction = new Transaction(transactionHex);
                await _transactionBuildHelper.AddFee(transaction, context);

                var signed = await _signatureApiProvider.SignTransaction(transaction.ToHex());
                var signedTr = new Transaction(signed);

                await _broadcastService.BroadcastTransaction(commitment.CommitmentId, signedTr);

                //await CloseChannel(commitment);

                return signedTr.GetHash().ToString();
            });
        }

        public async Task CloseChannel(ICommitment commitment)
        {
            await _lykkeTransactionBuilderService.SaveSpentOutputs(commitment.CommitmentId, new Transaction(commitment.InitialTransaction));
            await _offchainChannelRepository.CloseChannel(commitment.Multisig, commitment.AssetId, commitment.ChannelId);
            await _commitmentRepository.CloseCommitmentsOfChannel(commitment.Multisig, commitment.AssetId, commitment.ChannelId);
        }

        private async Task<decimal> SendToMultisig(BitcoinAddress @from, BitcoinAddress toMultisig, IAsset assetEntity, TransactionBuilder builder, TransactionBuildContext context, decimal amount)
        {
            if (OpenAssetsHelper.IsBitcoin(assetEntity.Id))
            {
                Money sendAmount;
                var unspentOutputs = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(from.ToWif(), 1)).ToList();

                if (amount < 0)
                    sendAmount = unspentOutputs.OfType<Coin>().DefaultIfEmpty().Sum(o => o?.Amount ?? Money.Zero);
                else
                    sendAmount = Money.FromUnit(amount, MoneyUnit.BTC);

                if (sendAmount > 0)
                    await _transactionBuildHelper.SendWithChange(builder, context, unspentOutputs, toMultisig, sendAmount, from);

                return sendAmount.ToDecimal(MoneyUnit.BTC);
            }
            else
            {
                var asset = new BitcoinAssetId(assetEntity.BlockChainAssetId, _connectionParams.Network).AssetId;
                long sendAmount;

                var unspentOutputs = (await _bitcoinOutputsService.GetColoredUnspentOutputs(from.ToWif(), asset, 1)).ToList();
                if (amount < 0)
                    sendAmount = unspentOutputs.Sum(o => o.Amount.Quantity);
                else
                    sendAmount = new AssetMoney(asset, amount, assetEntity.MultiplierPower).Quantity;
                if (sendAmount > 0)
                    _transactionBuildHelper.SendAssetWithChange(builder, context, unspentOutputs,
                        toMultisig, new AssetMoney(asset, sendAmount), @from);

                return new AssetMoney(asset, sendAmount).ToDecimal(assetEntity.MultiplierPower);
            }
        }



        private ICoin FindCoin(Transaction tr, string multisig, string walletRedeemScript, decimal amount, IAsset asset)
        {
            if (OpenAssetsHelper.IsBitcoin(asset.Id))
            {
                var money = new Money(amount, MoneyUnit.BTC);
                return tr.Outputs.AsCoins().FirstOrDefault(o => o.Amount == money &&
                        o.ScriptPubKey.GetDestinationAddress(_connectionParams.Network).ToWif() == multisig)
                        .ToScriptCoin(new Script(walletRedeemScript));
            }
            var assetMoney = new AssetMoney(new BitcoinAssetId(asset.BlockChainAssetId), amount, asset.MultiplierPower);
            uint markerPosition;
            var marker = ColorMarker.Get(tr, out markerPosition);
            var found = tr.Outputs.AsIndexedOutputs()
                .FirstOrDefault(o => o.TxOut.ScriptPubKey.GetDestinationAddress(_connectionParams.Network)?.ToWif() == multisig &&
                                     o.N > markerPosition && marker.Quantities[o.N - markerPosition - 1] == (ulong)assetMoney.Quantity);
            return found?.ToCoin().ToScriptCoin(new Script(walletRedeemScript)).ToColoredCoin(assetMoney);
        }


        private CreationCommitmentResult CreateCommitmentTransaction(IWalletAddress wallet, PubKey lockedPubKey, PubKey unlockedPubKey, PubKey revokePubKey, PubKey multisigPairPubKey,
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
            var script = OffchainScriptCommitmentTemplate.CreateOffchainScript(multisigPairPubKey, revokePubKey, lockedPubKey, OneDayDelay);

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
                    builder.SendAsset(lockedAddress, new AssetMoney(assetId, lockedAmount, asset.MultiplierPower));
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
            return new CreationCommitmentResult(tr, lockedAddress.ToWif(), script.ToHex());
        }

        private class CreationCommitmentResult
        {
            public Transaction Transaction { get; }
            public string LockedAddress { get; }
            public string LockedScript { get; }

            public CreationCommitmentResult(Transaction transaction, string lockedAddress, string lockedScript)
            {
                Transaction = transaction;
                LockedAddress = lockedAddress;
                LockedScript = lockedScript;
            }
        }
    }
}

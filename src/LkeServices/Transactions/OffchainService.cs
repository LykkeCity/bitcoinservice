﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Common.Log;
using Core.Perfomance;
using Core.Providers;
using Core.Repositories.Offchain;
using Core.Repositories.RevokeKeys;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Wallets;
using Core.ScriptTemplates;
using Core.Settings;
using LkeServices.Helpers;
using LkeServices.Providers;
using LkeServices.Signature;

namespace LkeServices.Transactions
{
    public interface IOffchainService
    {
        Task<OffchainResponse> CreateTransfer(string clientPubKey, decimal amount, IAsset asset, string clientPrevPrivateKey, bool requiredTransfer, Guid? transferId);

        Task<OffchainResponse> CreateUnsignedChannel(string clientPubKey, string hotWalletAddr, decimal hubAmount, IAsset asset, bool requiredTransfer, Guid? transferId);

        Task<OffchainResponse> CreateCashin(string clientPubKey, decimal amount, IAsset asset, string cashinAddress, Guid? transferId);

        Task<OffchainResponse> CreateHubCommitment(string clientPubKey, IAsset asset, decimal amount, string signedByClientChannel);

        Task<OffchainFinalizeResponse> Finalize(string clientPubKey, string hotWalletAddr, IAsset asset, string clientRevokePubKey, string signedByClientHubCommitment, Guid transferId);

        Task<CashoutOffchainResponse> CreateCashout(string clientPubKey, string cashoutAddress, string hotWalletAddr, decimal amount, IAsset asset);

        Task<decimal> GetClientBalance(string multisig, IAsset asset);

        Task<OffchainBalance> GetBalances(string multisig);

        Task<string> BroadcastClosingChannel(string clientPubKey, IAsset asset, string signedByClientTransaction);

        Task SpendCommitmemtByMultisig(ICommitment commitment, ICoin spendingCoin, string destination);

        Task<string> BroadcastCommitment(string clientPubKey, IAsset asset, string transaction);

        Task CloseChannel(ICommitment commitment);

        Task RemoveChannel(string multisig, IAsset asset);

        Task<bool> HasChannel(string multisig);

        Task<IEnumerable<OffchainChannelInfo>> GetChannelsOfAsset(string multisig, IAsset asset);

        Task<IEnumerable<OffchainCommitmentInfo>> GetCommitmentsOfChannel(Guid channelId);

        Task<string> GetCommitment(Guid commitmentId);
    }

    public class OffchainService : IOffchainService
    {
        private const int OneDayDelay = 6 * 24; // 24 hours * 6 blocks in hour
        private const SigHash CommitmentSignatureType = SigHash.All | SigHash.AnyoneCanPay;

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
        private readonly CachedDataDictionary<string, IAsset> _assetRepository;
        private readonly IPerfomanceMonitorFactory _perfomanceMonitorFactory;
        private readonly BaseSettings _settings;
        private readonly ILog _logger;
        private readonly IClosingChannelRepository _closingChannelRepository;

        public OffchainService(
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
            CachedDataDictionary<string, IAsset> assetRepository,
            IPerfomanceMonitorFactory perfomanceMonitorFactory,
            BaseSettings settings,
            ILog logger,
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
            _assetRepository = assetRepository;
            _perfomanceMonitorFactory = perfomanceMonitorFactory;
            _settings = settings;
            _logger = logger;
            _closingChannelRepository = closingChannelRepository;
        }

        private async Task CheckTransferFinalization(string multisig, string assetId, Guid? transferId, bool channelSetup, IPerfomanceMonitor monitor = null)
        {
            monitor?.Step("Get last transfer");
            var transfer = await _offchainTransferRepository.GetLastTransfer(multisig, assetId);
            if (transfer == null)
                return;
            if (transfer.Completed)
                return;
            if (!transfer.Completed && transfer.Required && transfer.TransferId != transferId)
                throw new BackendException("Channel is not finalized", ErrorCode.ChannelNotFinalized);
            monitor?.Step("Get channel");
            var channel = await _offchainChannelRepository.GetChannel(multisig, assetId);
            if (channel == null)
                return;

            monitor?.Step("Close transfer");
            await _offchainTransferRepository.CloseTransfer(multisig, assetId, transfer.TransferId);

            if (!channel.IsBroadcasted)
            {
                monitor?.Step("Revert channel");
                await _offchainChannelRepository.RevertChannel(multisig, assetId, channel.ChannelId);
                monitor?.Step("Remove commitments of channel");
                await _commitmentRepository.RemoveCommitmentsOfChannel(multisig, assetId, channel.ChannelId);
                monitor?.Step("Remove spent outputs");
                await _lykkeTransactionBuilderService.RemoveSpenOutputs(new Transaction(channel.InitialTransaction));

                if (!channelSetup)
                    throw new BackendException("Should open new channel", ErrorCode.ShouldOpenNewChannel);
            }
        }

        public async Task<OffchainResponse> CreateTransfer(string clientPubKey, decimal amount, IAsset asset, string clientPrevPrivateKey, bool requiredTransfer, Guid? transferId)
        {
            using (var monitor = _perfomanceMonitorFactory.Create("CreateTransfer"))
            {
                monitor.Step("Get multisig");
                var address = await _multisigService.GetMultisig(clientPubKey);

                if (address == null)
                    throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

                monitor.ChildProcess("Check finalization");

                await CheckTransferFinalization(address.MultisigAddress, asset.Id, transferId, false, monitor);

                monitor.CompleteLastProcess();

                monitor.Step("Get channel");
                var channel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);
                if (channel == null)
                    throw new BackendException("Channel is not found", ErrorCode.ShouldOpenNewChannel);

                if (amount < 0 && channel.ClientAmount < Math.Abs(amount))
                    throw new BackendException("Client amount in channel is low than required", ErrorCode.NotEnoughtClientFunds);

                if (amount > 0 && channel.HubAmount < amount)
                    throw new BackendException("Hub amount in channel is low than required", ErrorCode.ShouldOpenNewChannel);

                if (string.IsNullOrWhiteSpace(clientPrevPrivateKey))
                    throw new BackendException("Private key for previous commitment is required", ErrorCode.ShouldOpenNewChannel);

                monitor.Step("Get last commitment");
                var prevCommitment = await _commitmentRepository.GetLastCommitment(address.MultisigAddress, asset.Id, CommitmentType.Client);

                var secret = new BitcoinSecret(clientPrevPrivateKey);

                if (prevCommitment.RevokePubKey != secret.PubKey.ToHex())
                    throw new BackendException("Client private key for previous commitment is invalid", ErrorCode.ShouldOpenNewChannel);

                monitor.Step("Get and complete closing");
                var closing = await _closingChannelRepository.GetClosingChannel(address.MultisigAddress, asset.Id);
                if (closing != null)
                    await _closingChannelRepository.CompleteClosingChannel(address.MultisigAddress, asset.Id, closing.ClosingChannelId);

                monitor.Step("Save private key");
                await _revokeKeyRepository.AddPrivateKey(prevCommitment.RevokePubKey, clientPrevPrivateKey);

                var hubRevokeKey = new Key();

                var newHubAmount = channel.HubAmount - amount;
                var newClientAmount = channel.ClientAmount + amount;

                monitor.Step("Create commitment");
                var commitmentResult = CreateCommitmentTransaction(address, new PubKey(address.ExchangePubKey), new PubKey(clientPubKey).GetAddress(_connectionParams.Network), hubRevokeKey.PubKey, new PubKey(clientPubKey), asset,
                    newHubAmount, newClientAmount, channel.FullySignedChannel);

                monitor.Step("Create transfer");

                var transfer = await _offchainTransferRepository.CreateTransfer(address.MultisigAddress, asset.Id, false);

                monitor.Step("Save hub commitment and hub revoke key");

                await Task.WhenAll(
                    _commitmentRepository.CreateCommitment(CommitmentType.Hub, channel.ChannelId,
                        address.MultisigAddress, asset.Id,
                        hubRevokeKey.PubKey.ToHex(), commitmentResult.Transaction.ToHex(), newClientAmount,
                        newHubAmount, commitmentResult.LockedAddress, commitmentResult.LockedScript),

                    _revokeKeyRepository.AddRevokeKey(hubRevokeKey.PubKey.ToHex(), RevokeKeyType.Exchange,
                        hubRevokeKey.ToString(_connectionParams.Network))
                );
                if (requiredTransfer)
                {
                    monitor.Step("Require transfer");
                    await _offchainTransferRepository.RequirеTransfer(address.MultisigAddress, asset.Id, transfer.TransferId);
                }
                return new OffchainResponse
                {
                    TransactionHex = commitmentResult.Transaction.ToHex(),
                    TransferId = transfer.TransferId
                };
            }
        }

        public async Task<OffchainResponse> CreateUnsignedChannel(string clientPubKey, string hotWalletAddr, decimal hubAmount, IAsset asset, bool requiredTransfer, Guid? transferId)
        {
            return await Retry.Try(async () =>
            {
                using (var monitor = _perfomanceMonitorFactory.Create("Create channel"))
                {
                    monitor.Step("Get multisig");
                    var address = await _multisigService.GetMultisig(clientPubKey);

                    if (address == null)
                        throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

                    var multisig = new BitcoinScriptAddress(address.MultisigAddress, _connectionParams.Network);

                    var hotWalletAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(hotWalletAddr);

                    monitor.ChildProcess("Check finalization");
                    await CheckTransferFinalization(address.MultisigAddress, asset.Id, transferId, true, monitor);
                    monitor.CompleteLastProcess();

                    monitor.Step("Get channel");
                    var currentChannel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);

                    if (currentChannel != null && !currentChannel.IsBroadcasted)
                        throw new BackendException("There is another pending channel setup", ErrorCode.AnotherChannelSetupExists);

                    int fiatCoef = !OpenAssetsHelper.IsBitcoin(asset.Id) && !OpenAssetsHelper.IsLkk(asset.Id) && asset.IssueAllowed ? _settings.Offchain.FiatAssetAmountCoef : 1;

                    var context = _transactionBuildContextFactory.Create(_connectionParams.Network);
                    return await context.Build(async () =>
                    {
                        var builder = new TransactionBuilder();
                        // ReSharper disable AccessToDisposedClosure
                        monitor.Step("Send from multisig to multisig");

                        var multisigAmount = await SendToMultisig(multisig, multisig, asset, builder, context, -1);
                        decimal clientChannelAmount, hubChannelAmount;
                        if (currentChannel == null)
                        {
                            clientChannelAmount = multisigAmount;
                            monitor.Step("Send from hotwallet to multisig");
                            hubChannelAmount = await SendToMultisig(hotWalletAddress, multisig, asset, builder, context, hubAmount * fiatCoef);
                        }
                        else
                        {
                            clientChannelAmount = currentChannel.ClientAmount +
                                                  Math.Max(0, multisigAmount - currentChannel.ClientAmount - currentChannel.HubAmount);
                            monitor.Step("Send from hotwallet to multisig");
                            hubChannelAmount = await SendToMultisig(hotWalletAddress, multisig, asset, builder, context,
                                Math.Max(0, hubAmount - currentChannel.HubAmount) * fiatCoef);
                            hubChannelAmount += currentChannel.HubAmount;
                        }
                        monitor.Step("Add fee");
                        await _transactionBuildHelper.AddFee(builder, context);
                        var tr = builder.BuildTransaction(true);

                        _transactionBuildHelper.AggregateOutputs(tr);

                        var hex = tr.ToHex();
                        monitor.Step("Create transfer");
                        var transfer = await _offchainTransferRepository.CreateTransfer(multisig.ToWif(), asset.Id, false);
                        transferId = transfer.TransferId;
                        monitor.Step("Create channel");
                        var channel = await _offchainChannelRepository.CreateChannel(multisig.ToWif(), asset.Id, hex, clientChannelAmount,
                            hubChannelAmount);

                        monitor.Step("Save spent outputs");
                        await _lykkeTransactionBuilderService.SaveSpentOutputs(channel.ChannelId, tr);
                        monitor.Step("Save new outputs");
                        await SaveNewOutputs(tr, context, channel.ChannelId);

                        if (requiredTransfer)
                        {
                            monitor.Step("Require transfer");
                            await _offchainTransferRepository.RequirеTransfer(address.MultisigAddress, asset.Id, transfer.TransferId);
                        }

                        return new OffchainResponse
                        {
                            TransactionHex = hex,
                            TransferId = transfer.TransferId
                        };
                        // ReSharper restore AccessToDisposedClosure
                    });
                }
            }, exception => (exception as BackendException)?.Code == ErrorCode.TransactionConcurrentInputsProblem, 5, _logger);
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

                await SaveNewOutputs(tr, context, channel.ChannelId);

                return new OffchainResponse
                {
                    TransactionHex = hex,
                    TransferId = transfer.TransferId
                };
            });
        }

        public async Task<OffchainResponse> CreateHubCommitment(string clientPubKey, IAsset asset, decimal amount, string signedByClientChannel)
        {
            using (var monitor = _perfomanceMonitorFactory.Create("CreateHubCommitment"))
            {
                monitor.Step("Get multisig");
                var address = await _multisigService.GetMultisig(clientPubKey);

                if (address == null)
                    throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

                monitor.Step("Get channel");
                var channel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);
                if (channel == null)
                    throw new BackendException("Channel is not found", ErrorCode.ShouldOpenNewChannel);

                if (channel.IsBroadcasted)
                    throw new BackendException("Channel was broadcasted", ErrorCode.ChannelWasBroadcasted);

                if (amount < 0 && channel.ClientAmount < Math.Abs(amount))
                    throw new BackendException("Client amount in channel is low than required",
                        ErrorCode.NotEnoughtClientFunds);

                if (amount > 0 && channel.HubAmount < amount)
                    throw new BackendException("Hub amount in channel is low than required",
                        ErrorCode.ShouldOpenNewChannel);

                monitor.Step("Check signature");
                if (!TransactionComparer.CompareTransactions(channel.InitialTransaction, signedByClientChannel))
                    throw new BackendException("Provided signed transaction is not equal initial transaction",
                        ErrorCode.BadTransaction);

                if (!await _signatureVerifier.Verify(signedByClientChannel, clientPubKey))
                    throw new BackendException("Provided signed transaction is not signed by client",
                        ErrorCode.BadTransaction);

                monitor.Step("Sign channel creation");
                var fullSignedChannel = await _signatureApiProvider.SignTransaction(signedByClientChannel);

                if (!_signatureVerifier.VerifyScriptSigs(fullSignedChannel))
                    throw new BackendException("Channel transaction is not full signed",
                        ErrorCode.BadFullSignTransaction);

                monitor.Step("Save channel creation transaction");
                await _offchainChannelRepository.SetFullSignedTransaction(address.MultisigAddress, asset.Id, fullSignedChannel);

                var hubRevokeKey = new Key();

                var newHubAmount = channel.HubAmount - amount;
                var newClientAmount = channel.ClientAmount + amount;
                var commitmentResult = CreateCommitmentTransaction(address, new PubKey(address.ExchangePubKey),
                    new PubKey(clientPubKey).GetAddress(_connectionParams.Network), hubRevokeKey.PubKey,
                    new PubKey(clientPubKey), asset,
                    newHubAmount, newClientAmount, fullSignedChannel);

                monitor.Step("Save hub commitment and hub revoke key");

                await Task.WhenAll(
                    _commitmentRepository.CreateCommitment(CommitmentType.Hub, channel.ChannelId, address.MultisigAddress, asset.Id,
                        hubRevokeKey.PubKey.ToHex(), commitmentResult.Transaction.ToHex(), newClientAmount, newHubAmount, commitmentResult.LockedAddress, commitmentResult.LockedScript),
                    _revokeKeyRepository.AddRevokeKey(hubRevokeKey.PubKey.ToHex(), RevokeKeyType.Exchange, hubRevokeKey.ToString(_connectionParams.Network))
                );

                return new OffchainResponse
                {
                    TransactionHex = commitmentResult.Transaction.ToHex(),
                };
            }
        }

        public async Task<OffchainFinalizeResponse> Finalize(string clientPubKey, string hotWalletAddr, IAsset asset, string clientRevokePubKey, string signedByClientHubCommitment, Guid transferId)
        {
            using (var monitor = _perfomanceMonitorFactory.Create("Finalize"))
            {
                monitor.Step("Get multisig");
                var address = await _multisigService.GetMultisig(clientPubKey);

                if (address == null)
                    throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

                monitor.Step("Get channel");
                var channel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);
                if (channel == null)
                    throw new BackendException("Channel is not found", ErrorCode.ShouldOpenNewChannel);

                monitor.Step("Get last commitment");
                var hubCommitment = await _commitmentRepository.GetLastCommitment(address.MultisigAddress, asset.Id, CommitmentType.Hub);
                if (hubCommitment == null)
                    throw new BackendException("Commitment is not found", ErrorCode.CommitmentNotFound);

                monitor.Step("Get last transfer");
                var transfer = await _offchainTransferRepository.GetLastTransfer(address.MultisigAddress, asset.Id);
                if (transfer == null)
                    throw new BackendException("Transfer is not found", ErrorCode.TransferNotFound);
                if (transfer.TransferId != transferId)
                    throw new BackendException("Wrong transfer id", ErrorCode.WrongTransferId);

                monitor.Step("Get revoke key");
                if (await _revokeKeyRepository.GetRevokeKey(clientRevokePubKey) != null)
                    throw new BackendException("Client revoke public key was used already", ErrorCode.KeyUsedAlready);

                monitor.Step("Check signature");
                if (!TransactionComparer.CompareTransactions(signedByClientHubCommitment, hubCommitment.InitialTransaction))
                    throw new BackendException("Provided signed transaction is not equal initial transaction", ErrorCode.BadTransaction);

                if (!await _signatureVerifier.Verify(signedByClientHubCommitment, clientPubKey, CommitmentSignatureType))
                    throw new BackendException("Provided signed transaction is not signed by client", ErrorCode.BadTransaction);

                monitor.Step("Save signed hub commitment");
                await _commitmentRepository.SetFullSignedTransaction(hubCommitment.CommitmentId, address.MultisigAddress, asset.Id, signedByClientHubCommitment);

                monitor.Step("Create client commitment");
                var clientCommitmentResult = CreateCommitmentTransaction(address, new PubKey(clientPubKey),
                    OpenAssetsHelper.GetBitcoinAddressFormBase58Date(hotWalletAddr), new PubKey(clientRevokePubKey), new PubKey(address.ExchangePubKey), asset,
                    hubCommitment.ClientAmount, hubCommitment.HubAmount, channel.FullySignedChannel);

                monitor.Step("Sign client commitment");
                var signedByHubCommitment = await _signatureApiProvider.SignTransaction(clientCommitmentResult.Transaction.ToHex(), CommitmentSignatureType);

                monitor.Step("Save client commitment and client revoke key");

                await Task.WhenAll(
                    _commitmentRepository.CreateCommitment(CommitmentType.Client, channel.ChannelId,
                        address.MultisigAddress, asset.Id,
                        clientRevokePubKey, signedByHubCommitment, hubCommitment.ClientAmount, hubCommitment.HubAmount,
                        clientCommitmentResult.LockedAddress, clientCommitmentResult.LockedScript),
                    _revokeKeyRepository.AddRevokeKey(clientRevokePubKey, RevokeKeyType.Client)
                );
                string hash = null;
                if (!channel.IsBroadcasted)
                {
                    var channelTr = new Transaction(channel.FullySignedChannel);
                    hash = channelTr.GetHash().ToString();
                    monitor.ChildProcess("Broadcast transaction");
                    await _broadcastService.BroadcastTransaction(channel.ChannelId, channelTr, monitor);
                    monitor.CompleteLastProcess();

                    monitor.Step("Set channel broadcasted and close prev commitments");

                    await Task.WhenAll(
                        _offchainChannelRepository.SetChannelBroadcasted(address.MultisigAddress, asset.Id),
                        Task.Run(async () =>
                        {
                            if (channel.PrevChannelTransactionId.HasValue)
                            {
                                await _commitmentRepository.CloseCommitmentsOfChannel(address.MultisigAddress, asset.Id, channel.PrevChannelTransactionId.Value);
                            }
                        })
                    );
                }
                monitor.Step("Update amounts and complete transfer");

                await Task.WhenAll(
                    _offchainChannelRepository.UpdateAmounts(address.MultisigAddress, asset.Id, hubCommitment.ClientAmount, hubCommitment.HubAmount),
                    _offchainTransferRepository.CompleteTransfer(address.MultisigAddress, asset.Id, transfer.TransferId)
                );

                return new OffchainFinalizeResponse()
                {
                    TransactionHex = signedByHubCommitment,
                    Hash = hash
                };
            }
        }

        public async Task<CashoutOffchainResponse> CreateCashout(string clientPubKey, string cashoutAddr, string hotWalletAddr, decimal amount, IAsset asset)
        {
            var address = await _multisigService.GetMultisig(clientPubKey);

            if (address == null)
                throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

            var multisig = new BitcoinScriptAddress(address.MultisigAddress, _connectionParams.Network);

            var cashoutAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(cashoutAddr);
            var hotWalletAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(hotWalletAddr);

            await CheckTransferFinalization(address.MultisigAddress, asset.Id, null, true);

            var channel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);

            if (channel == null)
                return await CreateCashoutWithoutChannel(multisig, cashoutAddress, amount, asset);
            if (!channel.IsBroadcasted)
                throw new BackendException("There is another pending channel setup", ErrorCode.AnotherChannelSetupExists);

            if (channel.ClientAmount < amount)
                throw new BackendException("Client amount in channel is low than required", ErrorCode.NotEnoughtClientFunds);

            var isLkk = OpenAssetsHelper.IsLkk(asset.Id);
            var isBtc = OpenAssetsHelper.IsBitcoin(asset.Id);

            var isFullClosing = channel.ClientAmount == amount && (isLkk ||
                                                                   channel.HubAmount == 0 ||
                                                                   isBtc && channel.HubAmount > _settings.Offchain.OffchainDust);

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
                var savedHubAmount = channel.HubAmount;

                if (isBtc)
                {
                    if (amount > 0)
                        builder.Send(cashoutAddress, Money.FromUnit(amount, MoneyUnit.BTC));

                    if (channel.HubAmount > _settings.Offchain.OffchainDust)
                    {
                        builder.Send(hotWalletAddress, Money.FromUnit(channel.HubAmount, MoneyUnit.BTC));
                        savedHubAmount = 0;
                    }
                    if (amount < channel.ClientAmount || savedHubAmount > 0)
                        builder.Send(multisig, new Money(channel.ClientAmount - amount + savedHubAmount, MoneyUnit.BTC));
                }
                else
                {
                    if (isLkk)
                        savedHubAmount = 0;

                    var assetMoney = new AssetMoney(new BitcoinAssetId(asset.BlockChainAssetId).AssetId, amount, asset.MultiplierPower);
                    if (amount > 0)
                        builder.SendAsset(cashoutAddress, assetMoney);

                    if (channel.HubAmount > 0)
                        builder.SendAsset(isLkk ? hotWalletAddress : multisig,
                            new AssetMoney(new BitcoinAssetId(asset.BlockChainAssetId).AssetId, channel.HubAmount, asset.MultiplierPower));

                    if (amount < channel.ClientAmount)
                        builder.SendAsset(multisig,
                            new AssetMoney(new BitcoinAssetId(asset.BlockChainAssetId).AssetId, channel.ClientAmount - amount, asset.MultiplierPower));
                }

                await _transactionBuildHelper.AddFee(builder, context);

                var tr = builder.BuildTransaction(true);

                _transactionBuildHelper.AggregateOutputs(tr);

                var hex = tr.ToHex();
                if (!isFullClosing)
                {
                    var transfer = await _offchainTransferRepository.CreateTransfer(multisig.ToWif(), asset.Id, false);
                    var newChannel = await _offchainChannelRepository.CreateChannel(multisig.ToWif(), asset.Id, hex, channel.ClientAmount - amount, savedHubAmount);

                    await _lykkeTransactionBuilderService.SaveSpentOutputs(newChannel.ChannelId, tr);
                    await SaveNewOutputs(tr, context, newChannel.ChannelId);
                    return new CashoutOffchainResponse
                    {
                        TransactionHex = hex,
                        TransferId = transfer.TransferId
                    };
                }
                var closing = await _closingChannelRepository.CreateClosingChannel(address.MultisigAddress, asset.Id, channel.ChannelId, hex);

                await SaveNewOutputs(tr, context, closing.ClosingChannelId);

                return new CashoutOffchainResponse
                {
                    TransactionHex = hex,
                    TransferId = closing.ClosingChannelId,
                    ChannelClosed = true
                };
            });
        }

        private Task SaveNewOutputs(Transaction tr, TransactionBuildContext context, Guid transactionId)
        {
            return _broadcastedOutputRepository.InsertOutputs(OpenAssetsHelper.OrderBasedColoringOutputs(tr, context)
                       .Select(o => new BroadcastedOutput(o, transactionId, _connectionParams.Network)));
        }

        private async Task<CashoutOffchainResponse> CreateCashoutWithoutChannel(BitcoinScriptAddress multisig, BitcoinAddress cashoutAddress, decimal amount, IAsset asset)
        {
            if (amount == 0)
                throw new BackendException("Amount can't be equals to zero", ErrorCode.BadInputParameter);
            var context = _transactionBuildContextFactory.Create(_connectionParams.Network);

            var currentClosing = await _closingChannelRepository.GetClosingChannel(multisig.ToWif(), asset.Id);
            if (currentClosing != null)
                await _closingChannelRepository.CompleteClosingChannel(multisig.ToWif(), asset.Id, currentClosing.ClosingChannelId);


            return await context.Build(async () =>
            {
                var builder = new TransactionBuilder();
                try
                {
                    if (OpenAssetsHelper.IsBitcoin(asset.Id))
                    {
                        var unspentOutputs = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(multisig.ToWif())).ToList();
                        await _transactionBuildHelper.SendWithChange(builder, context, unspentOutputs, cashoutAddress, Money.FromUnit(amount, MoneyUnit.BTC),
                                multisig);
                    }
                    else
                    {
                        var assetId = new BitcoinAssetId(asset.BlockChainAssetId, _connectionParams.Network).AssetId;
                        var unspentOutputs = (await _bitcoinOutputsService.GetColoredUnspentOutputs(multisig.ToWif(), assetId)).ToList();

                        var sendAmount = new AssetMoney(assetId, amount, asset.MultiplierPower);
                        _transactionBuildHelper.SendAssetWithChange(builder, context, unspentOutputs, cashoutAddress, sendAmount, multisig);
                    }
                }
                catch (BackendException e) when (e.Code == ErrorCode.NotEnoughAssetAvailable || e.Code == ErrorCode.NotEnoughBitcoinAvailable)
                {
                    throw new BackendException("Client amount in channel is low than required", ErrorCode.NotEnoughtClientFunds);
                }
                await _transactionBuildHelper.AddFee(builder, context);
                var tr = builder.BuildTransaction(true);

                var hex = tr.ToHex();

                var closing = await _closingChannelRepository.CreateClosingChannel(multisig.ToWif(), asset.Id, Guid.Empty, hex);
                await SaveNewOutputs(tr, context, closing.ClosingChannelId);

                return new CashoutOffchainResponse
                {
                    TransactionHex = hex,
                    TransferId = closing.ClosingChannelId,
                    ChannelClosed = true
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

            if (channel != null && !channel.IsBroadcasted)
                throw new BackendException("There is another pending channel setup", ErrorCode.AnotherChannelSetupExists);

            var closing = await _closingChannelRepository.GetClosingChannel(address.MultisigAddress, asset.Id);

            if (closing == null)
                throw new BackendException("Closing channel is not found", ErrorCode.ClosingChannelNotFound);

            if (channel != null && closing.ChannelId != channel.ChannelId)
                throw new BackendException("Closing channel belong to expired channel", ErrorCode.ClosingChannelExpired);

            if (!TransactionComparer.CompareTransactions(closing.InitialTransaction, signedByClientTransaction))
                throw new BackendException("Provided signed transaction is not equal initial transaction", ErrorCode.BadTransaction);

            var fullSigned = await _signatureApiProvider.SignTransaction(signedByClientTransaction);

            var tr = new Transaction(fullSigned);

            await _broadcastService.BroadcastTransaction(closing.ClosingChannelId, tr);

            if (channel != null)
            {
                await _offchainChannelRepository.CloseChannel(address.MultisigAddress, asset.Id, channel.ChannelId);
                await _commitmentRepository.CloseCommitmentsOfChannel(address.MultisigAddress, asset.Id, channel.ChannelId);
            }

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

        public async Task RemoveChannel(string multisig, IAsset asset)
        {
            var channel = await _offchainChannelRepository.GetChannel(multisig, asset.Id);
            if (channel != null)
            {
                await _offchainChannelRepository.CloseChannel(multisig, asset.Id, channel.ChannelId);
                await _commitmentRepository.CloseCommitmentsOfChannel(multisig, asset.Id, channel.ChannelId);
            }
            var transfer = await _offchainTransferRepository.GetLastTransfer(multisig, asset.Id);
            if (transfer != null)
                await _offchainTransferRepository.CloseTransfer(multisig, asset.Id, transfer.TransferId);
        }

        public Task<bool> HasChannel(string multisig)
        {
            return _offchainChannelRepository.HasChannel(multisig);
        }

        public async Task<IEnumerable<OffchainChannelInfo>> GetChannelsOfAsset(string multisig, IAsset asset)
        {
            var channels = await _offchainChannelRepository.GetChannels(multisig, asset.Id);
            return channels.Select(o => new OffchainChannelInfo
            {
                ChannelId = o.ChannelId,
                Date = o.CreateDt,
                ClientAmount = o.ClientAmount,
                HubAmount = o.HubAmount,
                TransactionHash = o.IsBroadcasted ? new Transaction(o.FullySignedChannel).GetHash().ToString() : ""
            }).ToList();
        }

        public async Task<IEnumerable<OffchainCommitmentInfo>> GetCommitmentsOfChannel(Guid channelId)
        {
            var commitments = (await _commitmentRepository.GetCommitments(channelId)).OrderBy(o => o.CreateDt).ToList();
            if (!commitments.Any())
                return new List<OffchainCommitmentInfo>();

            var pairs = new List<ICommitment[]>();

            bool IsFullPair(ICommitment[] c)
            {
                if (c[0] == null || c[1] == null) return false;
                return c[0].ClientAmount == c[1].ClientAmount && c[0].HubAmount == c[1].HubAmount;
            }

            var pair = new ICommitment[2];
            foreach (var commitment in commitments)
            {
                pair[(int)commitment.Type - 1] = commitment;
                if (IsFullPair(pair))
                {
                    pairs.Add(pair);
                    pair = new ICommitment[2];
                }
            }

            return pairs.Select(o => new OffchainCommitmentInfo
            {
                ClientAmount = o[0].ClientAmount,
                HubAmount = o[0].HubAmount,
                Date = o[0].CreateDt,
                ClientCommitment = o[(int)CommitmentType.Client - 1].CommitmentId,
                HubCommitment = o[(int)CommitmentType.Hub - 1].CommitmentId
            });
        }

        public async Task<string> GetCommitment(Guid commitmentId)
        {
            var commitment = await _commitmentRepository.GetCommitment(commitmentId);
            return commitment?.SignedTransaction != null ? commitment.SignedTransaction : commitment?.InitialTransaction;
        }

        private async Task<decimal> SendToMultisig(BitcoinAddress @from, BitcoinAddress toMultisig, IAsset assetEntity, TransactionBuilder builder, TransactionBuildContext context, decimal amount)
        {
            if (amount == 0)
                return 0;
            if (OpenAssetsHelper.IsBitcoin(assetEntity.Id))
            {
                Money sendAmount;
                var unspentOutputs = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(from.ToWif())).ToList();

                if (amount < 0)
                    sendAmount = unspentOutputs.OfType<Coin>().DefaultIfEmpty().Sum(o => o?.Amount ?? Money.Zero);
                else
                    sendAmount = Money.FromUnit(amount, MoneyUnit.BTC);

                if (sendAmount > 0)
                    return await _transactionBuildHelper.SendWithChange(builder, context, unspentOutputs, toMultisig, sendAmount, from);

                return sendAmount.ToDecimal(MoneyUnit.BTC);
            }
            else
            {
                var asset = new BitcoinAssetId(assetEntity.BlockChainAssetId, _connectionParams.Network).AssetId;
                long sendAmount;

                var unspentOutputs = (await _bitcoinOutputsService.GetColoredUnspentOutputs(from.ToWif(), asset)).ToList();
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


        private CreationCommitmentResult CreateCommitmentTransaction(IWalletAddress wallet, PubKey lockedPubKey, IDestination unlockedAddress, PubKey revokePubKey, PubKey multisigPairPubKey,
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

        public async Task<decimal> GetClientBalance(string multisig, IAsset asset)
        {
            var channel = await _offchainChannelRepository.GetChannel(multisig, asset.Id);

            if (channel == null)
                throw new BackendException("Channel is not found", ErrorCode.ShouldOpenNewChannel);
            return channel.ClientAmount;
        }

        public async Task<OffchainBalance> GetBalances(string multisig)
        {
            var result = new OffchainBalance();
            var assets = await _assetRepository.Values();
            foreach (var asset in assets.Where(x => !x.IsDisabled && string.IsNullOrWhiteSpace(x.PartnerId)))
            {
                var channel = await _offchainChannelRepository.GetChannel(multisig, asset.Id);
                if (channel != null)
                {
                    string hash = null;
                    if (channel.IsBroadcasted)
                        hash = new Transaction(channel.FullySignedChannel).GetHash().ToString();
                    result.Channels[asset.Id] = new OffchainBalanceInfo
                    {
                        ClientAmount = channel.ClientAmount,
                        HubAmount = channel.HubAmount,
                        TransactionHash = hash
                    };
                }
            }
            return result;
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

    public class OffchainResponse
    {
        public Guid TransferId { get; set; }

        public string TransactionHex { get; set; }
    }


    public class OffchainFinalizeResponse : OffchainResponse
    {
        public string Hash { get; set; }
    }

    public class CashoutOffchainResponse : OffchainResponse
    {
        public bool ChannelClosed { get; set; }
    }

    public class OffchainBalance
    {
        public Dictionary<string, OffchainBalanceInfo> Channels { get; set; } = new Dictionary<string, OffchainBalanceInfo>();
    }

    public class OffchainBalanceInfo
    {
        public decimal ClientAmount { get; set; }

        public decimal HubAmount { get; set; }

        public string TransactionHash { get; set; }
    }

    public class OffchainChannelInfo : OffchainBalanceInfo
    {
        public Guid ChannelId { get; set; }

        public DateTime Date { get; set; }
    }

    public class OffchainCommitmentInfo
    {
        public DateTime Date { get; set; }

        public decimal ClientAmount { get; set; }

        public decimal HubAmount { get; set; }

        public Guid ClientCommitment { get; set; }

        public Guid HubCommitment { get; set; }
    }
}

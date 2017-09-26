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
using Core;
using Core.Outputs;
using Core.Performance;
using Core.Providers;
using Core.RabbitNotification;
using Core.Repositories.Offchain;
using Core.Repositories.RevokeKeys;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Wallets;
using Core.ScriptTemplates;
using Core.Settings;
using LkeServices.Helpers;
using LkeServices.Providers;
using LkeServices.Signature;
using NBitcoin.Policy;

namespace LkeServices.Transactions
{
    public interface IOffchainService
    {
        Task<OffchainResponse> CreateTransfer(string clientPubKey, decimal amount, IAsset asset, string clientPrevPrivateKey, bool requiredTransfer, Guid? transferId);

        Task<OffchainResponse> CreateUnsignedChannel(string clientPubKey, decimal hubAmount, IAsset asset, bool requiredTransfer, Guid? transferId, decimal? clientAmount);

        Task<OffchainResponse> CreateHubCommitment(string clientPubKey, IAsset asset, decimal amount, string signedByClientChannel);

        Task<OffchainFinalizeResponse> Finalize(string clientPubKey, IAsset asset, string clientRevokePubKey, string signedByClientHubCommitment, Guid transferId, Guid? notifyTxId = null);

        Task<CashoutOffchainResponse> CreateCashout(string clientPubKey, string cashoutAddress, decimal amount, IAsset asset);

        Task<CashoutOffchainResponse> CreateCashout(string clientPubKey, IAsset asset);

        Task<decimal> GetClientBalance(string multisig, IAsset asset);

        Task<OffchainBalance> GetBalances(string multisig);

        Task<string> BroadcastClosingChannel(string clientPubKey, IAsset asset, string signedByClientTransaction, Guid? notifyTxId = null);

        Task<string> SpendCommitmemtByMultisig(ICommitment commitment, ICoin spendingCoin, string destination);
        Task<string> SpendCommitmemtByPubkey(ICommitment commitment, ICoin spendingCoin, string destination);

        Task<string> BroadcastCommitment(string clientPubKey, IAsset asset, string transaction);
        Task<string> BroadcastCommitment(string multisig, IAsset asset);

        Task CloseChannel(ICommitment commitment);

        Task CloseChannel(IOffchainChannel channel);

        Task RemoveChannel(string multisig, IAsset asset);

        Task<bool> HasChannel(string multisig);

        Task<IEnumerable<OffchainChannelInfo>> GetChannelsOfAsset(string multisig, IAsset asset);

        Task<IEnumerable<OffchainCommitmentInfo>> GetCommitmentsOfChannel(Guid channelId);

        Task<string> GetCommitment(Guid commitmentId);

        Task<AssetBalanceInfo> GetAssetBalanceInfo(IAsset asset);

        Task<IAssetSetting> GetAssetSetting(string asset);

        Task<IEnumerable<IOffchainChannel>> GetCurrentChannels(string multisig);

        Task<IEnumerable<CommitmentBroadcastInfo>> GetCommitmentBroadcasts(int limit);
    }

    public class OffchainService : IOffchainService
    {
        public const int OneDayDelay = 6 * 24; // 24 hours * 6 blocks in hour
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
        private readonly IOffchainTransferRepository _offchainTransferRepository;
        private readonly TransactionBuildContextFactory _transactionBuildContextFactory;
        private readonly IBitcoinBroadcastService _broadcastService;
        private readonly CachedDataDictionary<string, IAsset> _assetRepository;
        private readonly CachedDataDictionary<string, IAssetSetting> _assetSettingCache;
        private readonly ISpentOutputService _spentOutputService;
        private readonly IPerformanceMonitorFactory _perfomanceMonitorFactory;
        private readonly BaseSettings _settings;
        private readonly ILog _logger;
        private readonly IAssetSettingRepository _assetSettingRepository;
        private readonly IReturnBroadcastedOutputsMessageWriter _returnBroadcastedOutputsMessageWriter;
        private readonly IRabbitNotificationService _rabbitNotificationService;
        private readonly ICommitmentBroadcastRepository _commitmentBroadcastRepository;
        private readonly ISpendCommitmentMonitoringWriter _spendCommitmentMonitoringWriter;
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
            IOffchainTransferRepository offchainTransferRepository,
            TransactionBuildContextFactory transactionBuildContextFactory,
            IBitcoinBroadcastService broadcastService,
            CachedDataDictionary<string, IAsset> assetRepository,
            CachedDataDictionary<string, IAssetSetting> assetSettingCache,
            ISpentOutputService spentOutputService,
            IPerformanceMonitorFactory perfomanceMonitorFactory,
            BaseSettings settings,
            ILog logger,
            IAssetSettingRepository assetSettingRepository,
            IReturnBroadcastedOutputsMessageWriter returnBroadcastedOutputsMessageWriter,
            IRabbitNotificationService rabbitNotificationService,
            ICommitmentBroadcastRepository commitmentBroadcastRepository,
            ISpendCommitmentMonitoringWriter spendCommitmentMonitoringWriter,
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
            _offchainTransferRepository = offchainTransferRepository;
            _transactionBuildContextFactory = transactionBuildContextFactory;
            _broadcastService = broadcastService;
            _assetRepository = assetRepository;
            _assetSettingCache = assetSettingCache;
            _spentOutputService = spentOutputService;
            _perfomanceMonitorFactory = perfomanceMonitorFactory;
            _settings = settings;
            _logger = logger;
            _assetSettingRepository = assetSettingRepository;
            _returnBroadcastedOutputsMessageWriter = returnBroadcastedOutputsMessageWriter;
            _rabbitNotificationService = rabbitNotificationService;
            _commitmentBroadcastRepository = commitmentBroadcastRepository;
            _spendCommitmentMonitoringWriter = spendCommitmentMonitoringWriter;
            _closingChannelRepository = closingChannelRepository;
        }

        private async Task CheckTransferFinalization(string multisig, string assetId, Guid? transferId, bool channelSetup, IPerformanceMonitor monitor = null)
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
                await _spentOutputService.RemoveSpenOutputs(new Transaction(channel.InitialTransaction));
                monitor?.Step("Return broadcasted outputs");
                await _returnBroadcastedOutputsMessageWriter.AddToReturn(channel.InitialTransaction, multisig);

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

                var assetSetting = await GetAssetSetting(asset.Id);


                if (amount < 0 && channel.ClientAmount < Math.Abs(amount))
                    throw new BackendException("Client amount in channel is low than required", ErrorCode.NotEnoughtClientFunds);

                if (amount > 0 && channel.HubAmount < amount)
                    throw new BackendException("Hub amount in channel is low than required", ErrorCode.ShouldOpenNewChannel);

                if (amount < 0 && channel.HubAmount - amount > assetSetting.MaxBalance)
                    throw new BackendException("Should open new channel", ErrorCode.ShouldOpenNewChannel);

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

        public async Task<OffchainResponse> CreateUnsignedChannel(string clientPubKey, decimal hubAmount, IAsset asset, bool requiredTransfer, Guid? transferId, decimal? clientAmount)
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

                    monitor.ChildProcess("Check finalization");
                    await CheckTransferFinalization(address.MultisigAddress, asset.Id, transferId, true, monitor);
                    monitor.CompleteLastProcess();

                    monitor.Step("Get channel");
                    var currentChannel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);

                    if (currentChannel != null && !currentChannel.IsBroadcasted)
                        throw new BackendException("There is another pending channel setup", ErrorCode.AnotherChannelSetupExists);

                    var assetSetting = await GetAssetSetting(asset.Id);

                    var context = _transactionBuildContextFactory.Create(_connectionParams.Network);
                    return await context.Build(async () =>
                    {
                        var builder = new TransactionBuilder();
                        // ReSharper disable AccessToDisposedClosure
                        monitor.Step("Send from multisig to multisig");

                        var multisigAmount = await SendToMultisig(multisig, multisig, asset, builder, context, -1, multisig, monitor);
                        decimal clientChannelAmount, hubChannelAmount;
                        if (currentChannel == null)
                        {
                            clientChannelAmount = multisigAmount;
                            monitor.Step("Send from hotwallet to multisig");
                            hubChannelAmount = await SendToMultisigFromHub(multisig, asset, builder, context, GetCorrectHubAmount(hubAmount, assetSetting), monitor);
                        }
                        else
                        {
                            clientChannelAmount = currentChannel.ClientAmount +
                                                  Math.Max(0, multisigAmount - currentChannel.ClientAmount - currentChannel.HubAmount);
                            monitor.Step("Send from hotwallet to multisig");
                            hubChannelAmount = await SendToMultisigFromHub(multisig, asset, builder, context, GetCorrectHubAmount(Math.Max(0, hubAmount - currentChannel.HubAmount), assetSetting), monitor);
                            hubChannelAmount += currentChannel.HubAmount;
                        }

                        if (clientChannelAmount < clientAmount.GetValueOrDefault())
                            throw new BackendException("Client amount in channel is low than required", ErrorCode.NotEnoughtClientFunds);

                        monitor.Step("Add fee");
                        await _transactionBuildHelper.AddFee(builder, context);
                        var tr = builder.BuildTransaction(true);

                        _transactionBuildHelper.AggregateOutputs(tr);

                        tr = ConstructAutoHubCashout(tr, hubChannelAmount, multisig, clientAmount, asset, assetSetting);

                        var hex = tr.ToHex();
                        monitor.Step("Create transfer");
                        var transfer = await _offchainTransferRepository.CreateTransfer(multisig.ToString(), asset.Id, false);
                        transferId = transfer.TransferId;
                        monitor.Step("Create channel");
                        var channel = await _offchainChannelRepository.CreateChannel(multisig.ToString(), asset.Id, hex, clientChannelAmount, hubChannelAmount);

                        monitor.Step("Save spent outputs");
                        await _spentOutputService.SaveSpentOutputs(channel.ChannelId, tr);
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

        private Transaction ConstructAutoHubCashout(Transaction tr, decimal hubChannelAmount, BitcoinScriptAddress multisig, decimal? clientAmount, IAsset asset, IAssetSetting assetSetting)
        {
            var transferFromClientAmount = clientAmount.GetValueOrDefault();
            var cashoutAddr = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(!string.IsNullOrEmpty(assetSetting.ChangeWallet)
                    ? assetSetting.ChangeWallet
                    : assetSetting.HotWallet);

            if (assetSetting.MaxBalance.HasValue && transferFromClientAmount > 0 && hubChannelAmount + transferFromClientAmount > assetSetting.MaxBalance)
            {
                if (OpenAssetsHelper.IsBitcoin(assetSetting.Asset))
                {
                    tr.Outputs.First(o => o.ScriptPubKey == multisig.ScriptPubKey).Value -= Money.FromUnit(transferFromClientAmount + hubChannelAmount, MoneyUnit.BTC);
                    tr.Outputs.Add(new TxOut(Money.FromUnit(transferFromClientAmount + hubChannelAmount, MoneyUnit.BTC), cashoutAddr));
                }
                else
                {
                    var assetId = new BitcoinAssetId(asset.BlockChainAssetId).AssetId;

                    var dust = new TxOut(Money.Zero, cashoutAddr.ScriptPubKey).GetDustThreshold(new StandardTransactionPolicy().MinRelayTxFee);

                    var marker = tr.GetColoredMarker();
                    var quantities = marker.Quantities.ToList();

                    while (quantities.Count < tr.Outputs.Count - 1)
                        quantities.Add(0);
                    var multisigIndex = (int)tr.Outputs.AsIndexedOutputs().First(o => o.TxOut.ScriptPubKey == multisig.ScriptPubKey).N;
                    var transferAmount = (ulong)new AssetMoney(assetId, transferFromClientAmount + hubChannelAmount, asset.MultiplierPower).Quantity;

                    quantities[multisigIndex - 1] -= transferAmount;
                    quantities.Add(transferAmount);
                    marker.Quantities = quantities.ToArray();

                    tr.Outputs.Add(new TxOut(dust, cashoutAddr));
                    tr.Outputs[0].ScriptPubKey = marker.GetScript();
                }
            }
            return tr;
        }


        private decimal GetCorrectHubAmount(decimal amount, IAssetSetting assetSetting)
        {
            if (amount > assetSetting.MaxBalance.GetValueOrDefault(decimal.MaxValue))
                return amount;
            var coefAmount = assetSetting.CashinCoef * amount;
            return Math.Min(coefAmount, assetSetting.MaxBalance.GetValueOrDefault(decimal.MaxValue));
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

                if (amount < 0)
                {
                    var assetSettings = await GetAssetSetting(asset.Id);
                    //hub amount was sent to change wallet in channel transaction
                    if (assetSettings.MaxBalance.HasValue && newHubAmount > assetSettings.MaxBalance)
                        newHubAmount = 0;
                }

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

        public async Task<OffchainFinalizeResponse> Finalize(string clientPubKey, IAsset asset, string clientRevokePubKey, string signedByClientHubCommitment, Guid transferId, Guid? notifyTxId = null)
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

                var assetSetting = await GetAssetSetting(asset.Id);

                var hotWallet = !string.IsNullOrEmpty(assetSetting.ChangeWallet)
                    ? OpenAssetsHelper.GetBitcoinAddressFormBase58Date(assetSetting.ChangeWallet)
                    : OpenAssetsHelper.GetBitcoinAddressFormBase58Date(assetSetting.HotWallet);

                monitor.Step("Create client commitment");
                var clientCommitmentResult = CreateCommitmentTransaction(address, new PubKey(clientPubKey), hotWallet, new PubKey(clientRevokePubKey), new PubKey(address.ExchangePubKey), asset,
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
                    await _broadcastService.BroadcastTransaction(channel.ChannelId, channelTr, monitor, notifyTxId: notifyTxId);
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
                    _rabbitNotificationService.OpenChannel(channel.ChannelId.ToString(), hash, asset.BlockChainAssetId,
                        address.MultisigAddress, new PubKey(address.ClientPubKey).ToString(_connectionParams.Network),
                        new PubKey(address.ExchangePubKey).ToString(_connectionParams.Network));
                }
                monitor.Step("Update amounts and complete transfer");

                await Task.WhenAll(
                    _offchainChannelRepository.UpdateAmounts(address.MultisigAddress, asset.Id, hubCommitment.ClientAmount, hubCommitment.HubAmount),
                    _offchainTransferRepository.CompleteTransfer(address.MultisigAddress, asset.Id, transfer.TransferId)
                );


                _rabbitNotificationService.Transfer(channel.ChannelId.ToString(), (notifyTxId ?? transfer.TransferId).ToString(), hubCommitment.ClientAmount, hubCommitment.HubAmount, DateTime.UtcNow);

                return new OffchainFinalizeResponse()
                {
                    TransactionHex = signedByHubCommitment,
                    Hash = hash
                };
            }
        }

        public async Task<CashoutOffchainResponse> CreateCashout(string clientPubKey, string cashoutAddr, decimal amount, IAsset asset)
        {
            var address = await _multisigService.GetMultisig(clientPubKey);

            if (address == null)
                throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

            var multisig = new BitcoinScriptAddress(address.MultisigAddress, _connectionParams.Network);

            var cashoutAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(cashoutAddr);


            await CheckTransferFinalization(address.MultisigAddress, asset.Id, null, true);

            var channel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);

            if (channel == null)
                return await CreateCashoutWithoutChannel(multisig, cashoutAddress, amount, asset);
            if (!channel.IsBroadcasted)
                throw new BackendException("There is another pending channel setup", ErrorCode.AnotherChannelSetupExists);

            if (channel.ClientAmount < amount)
                throw new BackendException("Client amount in channel is low than required", ErrorCode.NotEnoughtClientFunds);

            var assetSetting = await GetAssetSetting(asset.Id);

            var hotWallet = !string.IsNullOrEmpty(assetSetting.ChangeWallet)
                ? OpenAssetsHelper.GetBitcoinAddressFormBase58Date(assetSetting.ChangeWallet)
                : OpenAssetsHelper.GetBitcoinAddressFormBase58Date(assetSetting.HotWallet);

            var isBtc = OpenAssetsHelper.IsBitcoin(asset.Id);
            var cashoutFromHub = !asset.IssueAllowed && channel.HubAmount > assetSetting.Dust;

            var btcDust = new TxOut(Money.Zero, multisig.ScriptPubKey).GetDustThreshold(new StandardTransactionPolicy().MinRelayTxFee).ToDecimal(MoneyUnit.BTC);

            var isFullClosing = channel.ClientAmount == amount && (channel.HubAmount == 0 || cashoutFromHub) ||
                                channel.ClientAmount - amount + channel.HubAmount < btcDust && isBtc;

            var context = _transactionBuildContextFactory.Create(_connectionParams.Network);

            var currentClosing = await _closingChannelRepository.GetClosingChannel(address.MultisigAddress, asset.Id);
            if (currentClosing != null)
                await _closingChannelRepository.CompleteClosingChannel(address.MultisigAddress, asset.Id, currentClosing.ClosingChannelId);

            return await context.Build(async () =>
            {
                var builder = new TransactionBuilder();

                var coin = FindCoin(new Transaction(channel.FullySignedChannel), multisig.ToString(), address.RedeemScript,
                    channel.ClientAmount + channel.HubAmount, asset);

                builder.AddCoins(coin);
                var savedHubAmount = channel.HubAmount;

                if (isBtc)
                {
                    if (amount > 0)
                        builder.Send(cashoutAddress, Money.FromUnit(amount, MoneyUnit.BTC));

                    if (cashoutFromHub)
                    {
                        builder.Send(hotWallet, Money.FromUnit(channel.HubAmount, MoneyUnit.BTC));
                        savedHubAmount = 0;
                    }
                    if (amount < channel.ClientAmount || savedHubAmount > 0)
                        builder.Send(multisig, new Money(channel.ClientAmount - amount + savedHubAmount, MoneyUnit.BTC));
                }
                else
                {
                    var assetMoney = new AssetMoney(new BitcoinAssetId(asset.BlockChainAssetId).AssetId, amount, asset.MultiplierPower);
                    if (amount > 0)
                        builder.SendAsset(cashoutAddress, assetMoney);

                    if (cashoutFromHub)
                    {
                        builder.SendAsset(hotWallet, new AssetMoney(new BitcoinAssetId(asset.BlockChainAssetId).AssetId, channel.HubAmount, asset.MultiplierPower));
                        savedHubAmount = 0;
                    }

                    if (amount < channel.ClientAmount || savedHubAmount > 0)
                        builder.SendAsset(multisig,
                            new AssetMoney(new BitcoinAssetId(asset.BlockChainAssetId).AssetId, channel.ClientAmount - amount + savedHubAmount, asset.MultiplierPower));
                }


                await _transactionBuildHelper.AddFee(builder, context);

                var tr = builder.BuildTransaction(true);

                _transactionBuildHelper.AggregateOutputs(tr);

                var hex = tr.ToHex();
                if (!isFullClosing)
                {
                    var transfer = await _offchainTransferRepository.CreateTransfer(multisig.ToString(), asset.Id, false);
                    var newChannel = await _offchainChannelRepository.CreateChannel(multisig.ToString(), asset.Id, hex, channel.ClientAmount - amount, savedHubAmount);

                    await _spentOutputService.SaveSpentOutputs(newChannel.ChannelId, tr);
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

        public async Task<CashoutOffchainResponse> CreateCashout(string clientPubKey, IAsset asset)
        {
            var address = await _multisigService.GetMultisig(clientPubKey);

            if (address == null)
                throw new BackendException($"Client {clientPubKey} is not registered", ErrorCode.BadInputParameter);

            var multisig = new BitcoinScriptAddress(address.MultisigAddress, _connectionParams.Network);

            var assetSetting = await GetAssetSetting(asset.Id);

            var hotWallet = !string.IsNullOrEmpty(assetSetting.ChangeWallet)
                ? OpenAssetsHelper.GetBitcoinAddressFormBase58Date(assetSetting.ChangeWallet)
                : OpenAssetsHelper.GetBitcoinAddressFormBase58Date(assetSetting.HotWallet);

            await CheckTransferFinalization(address.MultisigAddress, asset.Id, null, false);

            var channel = await _offchainChannelRepository.GetChannel(address.MultisigAddress, asset.Id);

            if (channel == null)
                throw new BackendException("Channel is not found", ErrorCode.ShouldOpenNewChannel);

            if (!channel.IsBroadcasted)
                throw new BackendException("There is another pending channel setup", ErrorCode.AnotherChannelSetupExists);

            if (channel.HubAmount == 0)
                throw new BackendException("No coins to refund", ErrorCode.NoCoinsToRefund);

            bool isFullClosing = channel.ClientAmount == 0;

            var context = _transactionBuildContextFactory.Create(_connectionParams.Network);

            var currentClosing = await _closingChannelRepository.GetClosingChannel(address.MultisigAddress, asset.Id);
            if (currentClosing != null)
                await _closingChannelRepository.CompleteClosingChannel(address.MultisigAddress, asset.Id, currentClosing.ClosingChannelId);

            return await context.Build(async () =>
            {
                var builder = new TransactionBuilder();

                var coin = FindCoin(new Transaction(channel.FullySignedChannel), multisig.ToString(), address.RedeemScript,
                    channel.ClientAmount + channel.HubAmount, asset);

                builder.AddCoins(coin);

                bool isBtc = OpenAssetsHelper.IsBitcoin(asset.Id);
                var hubAmount = 0M;
                if (isBtc)
                {
                    var multisigDust = new TxOut(Money.Zero, multisig.ScriptPubKey).GetDustThreshold(builder.StandardTransactionPolicy.MinRelayTxFee);

                    var multisigAmount = new Money(channel.ClientAmount, MoneyUnit.BTC);
                    var cashoutAmount = Money.FromUnit(channel.HubAmount, MoneyUnit.BTC);

                    if (multisigAmount > 0 && multisigAmount < multisigDust)
                    {
                        cashoutAmount -= (multisigDust - multisigAmount);
                        multisigAmount = multisigDust;
                    }

                    builder.Send(hotWallet, cashoutAmount);
                    hubAmount = channel.HubAmount - cashoutAmount.ToDecimal(MoneyUnit.BTC);

                    if (multisigAmount > 0)
                        builder.Send(multisig, multisigAmount);
                }
                else
                {
                    var assetId = new BitcoinAssetId(asset.BlockChainAssetId).AssetId;
                    builder.SendAsset(hotWallet, new AssetMoney(assetId, channel.HubAmount, asset.MultiplierPower));
                    if (channel.ClientAmount > 0)
                        builder.SendAsset(multisig, new AssetMoney(assetId, channel.ClientAmount, asset.MultiplierPower));
                }


                await _transactionBuildHelper.AddFee(builder, context);

                var tr = builder.BuildTransaction(true);

                _transactionBuildHelper.AggregateOutputs(tr);

                var hex = tr.ToHex();
                if (!isFullClosing)
                {
                    var transfer = await _offchainTransferRepository.CreateTransfer(multisig.ToString(), asset.Id, false);
                    var newChannel = await _offchainChannelRepository.CreateChannel(multisig.ToString(), asset.Id, hex, channel.ClientAmount, hubAmount);

                    await _spentOutputService.SaveSpentOutputs(newChannel.ChannelId, tr);
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

            var currentClosing = await _closingChannelRepository.GetClosingChannel(multisig.ToString(), asset.Id);
            if (currentClosing != null)
                await _closingChannelRepository.CompleteClosingChannel(multisig.ToString(), asset.Id, currentClosing.ClosingChannelId);


            return await context.Build(async () =>
            {
                var builder = new TransactionBuilder();
                try
                {
                    if (OpenAssetsHelper.IsBitcoin(asset.Id))
                    {
                        var unspentOutputs = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(multisig.ToString())).ToList();
                        await _transactionBuildHelper.SendWithChange(builder, context, unspentOutputs, cashoutAddress, Money.FromUnit(amount, MoneyUnit.BTC),
                                multisig);
                    }
                    else
                    {
                        var assetId = new BitcoinAssetId(asset.BlockChainAssetId, _connectionParams.Network).AssetId;
                        var unspentOutputs = (await _bitcoinOutputsService.GetColoredUnspentOutputs(multisig.ToString(), assetId)).ToList();

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

                var closing = await _closingChannelRepository.CreateClosingChannel(multisig.ToString(), asset.Id, Guid.Empty, hex);
                await SaveNewOutputs(tr, context, closing.ClosingChannelId);

                return new CashoutOffchainResponse
                {
                    TransactionHex = hex,
                    TransferId = closing.ClosingChannelId,
                    ChannelClosed = true
                };
            });
        }


        public async Task<string> BroadcastClosingChannel(string clientPubKey, IAsset asset, string signedByClientTransaction, Guid? notifyTxId = null)
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

            await _broadcastService.BroadcastTransaction(closing.ClosingChannelId, tr, notifyTxId: notifyTxId);

            if (channel != null)
            {
                await _offchainChannelRepository.CloseChannel(address.MultisigAddress, asset.Id, channel.ChannelId);
                await _commitmentRepository.CloseCommitmentsOfChannel(address.MultisigAddress, asset.Id, channel.ChannelId);
            }

            await _closingChannelRepository.CompleteClosingChannel(address.MultisigAddress, asset.Id, closing.ClosingChannelId);

            await _spentOutputService.SaveSpentOutputs(closing.ClosingChannelId, tr);

            var hash = tr.GetHash().ToString();
            _rabbitNotificationService.CloseChannel(closing.ChannelId.ToString(), hash);

            return hash;
        }

        public async Task<string> SpendCommitmemtByMultisig(ICommitment commitment, ICoin spendingCoin, string destination)
        {
            TransactionBuildContext context = _transactionBuildContextFactory.Create(_connectionParams.Network);

            var destinationAddress = BitcoinAddress.Create(destination);

            return await context.Build(async () =>
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

                    tr.Inputs.First(o => o.PrevOut == spendingCoin.Outpoint).ScriptSig = OffchainScriptCommitmentTemplate.GenerateScriptSig(scriptParams);

                    var signed = await _signatureApiProvider.SignTransaction(tr.ToHex(), additionalSecrets: new[] { privateRevokeKey });

                    var signedTr = new Transaction(signed);
                    var id = Guid.NewGuid();
                    await _broadcastService.BroadcastTransaction(id, signedTr);

                    await _spentOutputService.SaveSpentOutputs(id, signedTr);

                    return signedTr.GetHash().ToString();
                });
        }


        public async Task<string> SpendCommitmemtByPubkey(ICommitment commitment, ICoin spendingCoin, string destination)
        {
            var context = _transactionBuildContextFactory.Create(_connectionParams.Network);

            var destinationAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(destination);

            return await context.Build(async () =>
            {
                var builder = new TransactionBuilder();
                builder.AddCoins(spendingCoin);
                if (OpenAssetsHelper.IsBitcoin(commitment.AssetId))
                    builder.Send(destinationAddress, spendingCoin.Amount);
                else
                    builder.SendAsset(destinationAddress, ((ColoredCoin)spendingCoin).Amount);
                await _transactionBuildHelper.AddFee(builder, context);

                var tr = builder.BuildTransaction(false);
                tr.Inputs.First(o => o.PrevOut == spendingCoin.Outpoint).Sequence = new Sequence(OneDayDelay);
                tr.Version = 2;

                var redeem = commitment.LockedScript.ToScript();
                var scriptParams = new OffchainScriptParams
                {
                    IsMultisig = false,
                    RedeemScript = redeem.ToBytes(),
                    Pushes = new[] { new byte[0] }
                };

                tr.Inputs.First(o => o.PrevOut == spendingCoin.Outpoint).ScriptSig = OffchainScriptCommitmentTemplate.GenerateScriptSig(scriptParams);

                var signed = await _signatureApiProvider.SignTransaction(tr.ToHex());

                var signedTr = new Transaction(signed);
                var id = Guid.NewGuid();
                await _broadcastService.BroadcastTransaction(id, signedTr);

                await _spentOutputService.SaveSpentOutputs(id, signedTr);

                return signedTr.GetHash().ToString();
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

            if (commitment.CommitmentId != lastCommitment.CommitmentId)
                throw new BackendException("Commitment is expired", ErrorCode.CommitmentExpired);

            TransactionBuildContext context = _transactionBuildContextFactory.Create(_connectionParams.Network);

            return await context.Build(async () =>
            {
                var transaction = new Transaction(transactionHex);
                await _transactionBuildHelper.AddFeeWithoutChange(transaction, context, 1);

                var signed = await _signatureApiProvider.SignTransaction(transaction.ToHex());
                var signedTr = new Transaction(signed);

                var hash = signedTr.GetHash().ToString();

                await _broadcastService.BroadcastTransaction(commitment.CommitmentId, signedTr);

                if (commitment.Type == CommitmentType.Hub && commitment.HubAmount > 0)
                    await _spendCommitmentMonitoringWriter.AddToMonitoring(commitment.CommitmentId, hash);

                await _commitmentBroadcastRepository.InsertCommitmentBroadcast(commitment.CommitmentId, hash,
                    CommitmentBroadcastType.Valid, commitment.ClientAmount, commitment.HubAmount, commitment.ClientAmount, commitment.HubAmount, null);

                await CloseChannel(commitment);

                return hash;
            });
        }

        public async Task<string> BroadcastCommitment(string multisig, IAsset asset)
        {
            var address = await _multisigService.GetMultisigByAddr(multisig);

            if (address == null)
                throw new BackendException($"Multisig is not registered", ErrorCode.BadInputParameter);

            var lastCommitment = await _commitmentRepository.GetLastCommitment(address.MultisigAddress, asset.Id, CommitmentType.Hub);
            return await BroadcastCommitment(address.ClientPubKey, asset, lastCommitment.SignedTransaction);
        }

        public async Task CloseChannel(ICommitment commitment)
        {
            await _spentOutputService.SaveSpentOutputs(commitment.CommitmentId, new Transaction(commitment.InitialTransaction));
            await _offchainChannelRepository.CloseChannel(commitment.Multisig, commitment.AssetId, commitment.ChannelId);
            await _commitmentRepository.CloseCommitmentsOfChannel(commitment.Multisig, commitment.AssetId, commitment.ChannelId);
        }

        public async Task CloseChannel(IOffchainChannel channel)
        {
            if (channel != null)
            {
                await _offchainChannelRepository.CloseChannel(channel.Multisig, channel.Asset, channel.ChannelId);
                await _commitmentRepository.CloseCommitmentsOfChannel(channel.Multisig, channel.Asset, channel.ChannelId);
            }
        }

        public async Task RemoveChannel(string multisig, IAsset asset)
        {
            var channel = await _offchainChannelRepository.GetChannel(multisig, asset.Id);
            await CloseChannel(channel);
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

        public async Task<AssetBalanceInfo> GetAssetBalanceInfo(IAsset asset)
        {
            var channels = await _offchainChannelRepository.GetChannels(asset.Id);

            return new AssetBalanceInfo
            {
                Balances = channels.Select(o => new MultisigBalanceInfo
                {
                    Multisig = o.Multisig,
                    HubAmount = o.HubAmount,
                    ClientAmount = o.ClientAmount
                }).ToList()
            };
        }

        private async Task<decimal> SendToMultisig(BitcoinAddress @from, BitcoinAddress toMultisig, IAsset assetEntity, TransactionBuilder builder, TransactionBuildContext context, decimal amount, IDestination changeDestination, IPerformanceMonitor monitor)
        {
            if (amount == 0)
                return 0;
            if (OpenAssetsHelper.IsBitcoin(assetEntity.Id))
            {
                Money sendAmount;

                monitor.ChildProcess("Get uncolored outputs");
                var unspentOutputs = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(from.ToString(), monitor: monitor)).ToList();
                monitor.CompleteLastProcess();

                if (amount < 0)
                    sendAmount = unspentOutputs.OfType<Coin>().DefaultIfEmpty().Sum(o => o?.Amount ?? Money.Zero);
                else
                    sendAmount = Money.FromUnit(amount, MoneyUnit.BTC);

                if (sendAmount > 0)
                    return await _transactionBuildHelper.SendWithChange(builder, context, unspentOutputs, toMultisig, sendAmount, changeDestination);

                return sendAmount.ToDecimal(MoneyUnit.BTC);
            }
            else
            {
                var asset = new BitcoinAssetId(assetEntity.BlockChainAssetId, _connectionParams.Network).AssetId;
                long sendAmount;

                monitor.ChildProcess("Get colored outputs");
                var unspentOutputs = (await _bitcoinOutputsService.GetColoredUnspentOutputs(from.ToString(), asset, monitor: monitor)).ToList();
                monitor.CompleteLastProcess();

                if (amount < 0)
                    sendAmount = unspentOutputs.Sum(o => o.Amount.Quantity);
                else
                    sendAmount = new AssetMoney(asset, amount, assetEntity.MultiplierPower).Quantity;
                if (sendAmount > 0)
                    _transactionBuildHelper.SendAssetWithChange(builder, context, unspentOutputs,
                        toMultisig, new AssetMoney(asset, sendAmount), changeDestination);

                return new AssetMoney(asset, sendAmount).ToDecimal(assetEntity.MultiplierPower);
            }
        }

        private async Task<IAssetSetting> CreateAssetSetting(string assetId, IAssetSetting defaultSetttings)
        {
            var clone = defaultSetttings.Clone(assetId);
            clone.ChangeWallet = defaultSetttings.ChangeWallet;
            clone.HotWallet = defaultSetttings.ChangeWallet;
            try
            {
                await _assetSettingRepository.Insert(clone);
            }
            catch
            {
                return await _assetSettingRepository.GetAssetSetting(assetId) ?? defaultSetttings;
            }
            return clone;
        }

        private async Task<decimal> SendToMultisigFromHub(BitcoinAddress toMultisig, IAsset assetEntity, TransactionBuilder builder, TransactionBuildContext context, decimal amount, IPerformanceMonitor monitor)
        {
            if (amount <= 0)
                return 0;
            var assetSetting = await GetAssetSetting(assetEntity.Id);

            var hotWalletAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(assetSetting.HotWallet);

            var changeAddress = !string.IsNullOrEmpty(assetSetting.ChangeWallet)
                ? OpenAssetsHelper.GetBitcoinAddressFormBase58Date(assetSetting.ChangeWallet)
                : hotWalletAddress;

            if (OpenAssetsHelper.IsBitcoin(assetEntity.Id))
            {
                decimal sentAmount = 0;
                monitor.ChildProcess("Get uncolored outputs");
                var unspentOutputs = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(hotWalletAddress.ToString(), monitor: monitor)).ToList();
                monitor.CompleteLastProcess();

                var neededAmount = Money.FromUnit(amount, MoneyUnit.BTC);
                var availableAmount = unspentOutputs.Cast<Coin>().DefaultIfEmpty().Select(o => o?.Amount ?? Money.Zero).Sum();

                if (availableAmount < neededAmount && changeAddress != hotWalletAddress)
                {
                    if (assetSetting.Asset == Constants.DefaultAssetSetting)
                        assetSetting = await CreateAssetSetting(assetEntity.Id, assetSetting);

                    monitor.Step("Get uncolored outputs from new hotwallet");
                    var outputs = (await _bitcoinOutputsService.GetUncoloredUnspentOutputs(changeAddress.ToString(), monitor: monitor)).ToList();
                    sentAmount = await _transactionBuildHelper.SendWithChange(builder, context, outputs, toMultisig, neededAmount - availableAmount, changeAddress);

                    if (assetSetting.Asset != Constants.DefaultAssetSetting)
                    {
                        monitor.Step("Update hotwallet");
                        await _assetSettingRepository.UpdateHotWallet(assetSetting.Asset, assetSetting.ChangeWallet);
                    }
                    neededAmount = availableAmount;
                }

                if (neededAmount > 0)
                    sentAmount += await _transactionBuildHelper.SendWithChange(builder, context, unspentOutputs, toMultisig, neededAmount, changeAddress);

                return sentAmount;
            }
            else
            {
                var asset = new BitcoinAssetId(assetEntity.BlockChainAssetId, _connectionParams.Network).AssetId;

                monitor.ChildProcess("Get colored outputs");
                var unspentOutputs = (await _bitcoinOutputsService.GetColoredUnspentOutputs(hotWalletAddress.ToString(), asset, monitor: monitor)).ToList();
                monitor.CompleteLastProcess();

                var neededAmount = new AssetMoney(asset, amount, assetEntity.MultiplierPower).Quantity;
                var availableAmount = unspentOutputs.Select(o => o.Amount.Quantity).DefaultIfEmpty().Sum();

                if (availableAmount < neededAmount && changeAddress != hotWalletAddress)
                {
                    if (assetSetting.Asset == Constants.DefaultAssetSetting)
                        assetSetting = await CreateAssetSetting(assetEntity.Id, assetSetting);

                    monitor.Step("Get colored outputs from new hotwallet");
                    var outputs = (await _bitcoinOutputsService.GetColoredUnspentOutputs(changeAddress.ToString(), asset, monitor: monitor)).ToList();
                    _transactionBuildHelper.SendAssetWithChange(builder, context, outputs, toMultisig, new AssetMoney(asset, neededAmount - availableAmount), changeAddress);

                    if (assetSetting.Asset != Constants.DefaultAssetSetting)
                    {
                        monitor.Step("Update hotwallet");
                        await _assetSettingRepository.UpdateHotWallet(assetSetting.Asset, assetSetting.ChangeWallet);
                    }
                    neededAmount = availableAmount;
                }
                if (neededAmount > 0)
                    _transactionBuildHelper.SendAssetWithChange(builder, context, unspentOutputs, toMultisig, new AssetMoney(asset, neededAmount), changeAddress);

                return new AssetMoney(asset, neededAmount).ToDecimal(assetEntity.MultiplierPower);
            }
        }

        private ICoin FindCoin(Transaction tr, string multisig, string walletRedeemScript, decimal amount, IAsset asset)
        {
            if (OpenAssetsHelper.IsBitcoin(asset.Id))
            {
                var money = new Money(amount, MoneyUnit.BTC);
                return tr.Outputs.AsCoins().FirstOrDefault(o => o.Amount == money &&
                        o.ScriptPubKey.GetDestinationAddress(_connectionParams.Network).ToString() == multisig)
                        ?.ToScriptCoin(new Script(walletRedeemScript));
            }
            var assetMoney = new AssetMoney(new BitcoinAssetId(asset.BlockChainAssetId), amount, asset.MultiplierPower);
            uint markerPosition;
            var marker = ColorMarker.Get(tr, out markerPosition);
            var found = tr.Outputs.AsIndexedOutputs()
                .FirstOrDefault(o => o.TxOut.ScriptPubKey.GetDestinationAddress(_connectionParams.Network)?.ToString() == multisig &&
                                     o.N > markerPosition && marker.Quantities[o.N - markerPosition - 1] == (ulong)assetMoney.Quantity);
            return found?.ToCoin().ToScriptCoin(new Script(walletRedeemScript)).ToColoredCoin(assetMoney);
        }


        private CreationCommitmentResult CreateCommitmentTransaction(IWalletAddress wallet, PubKey lockedPubKey, IDestination unlockedAddress, PubKey revokePubKey, PubKey multisigPairPubKey,
            IAsset asset, decimal lockedAmount, decimal unlockedAmount, string channelTr)
        {
            var multisig = new BitcoinScriptAddress(wallet.MultisigAddress, _connectionParams.Network);
            var channel = new Transaction(channelTr);
            var spendCoin = FindCoin(channel, multisig.ToString(), wallet.RedeemScript, lockedAmount + unlockedAmount, asset);

            if (spendCoin == null)
                throw new BackendException($"Not found output in setup channel with amount {lockedAmount + unlockedAmount}", ErrorCode.NoCoinsFound);

            TransactionBuilder builder = new TransactionBuilder();
            builder.AddCoins(spendCoin);
            long additionalBtc = 0;
            var script = OffchainScriptCommitmentTemplate.CreateOffchainScript(multisigPairPubKey, revokePubKey, lockedPubKey, OneDayDelay);

            var lockedAddress = script.GetScriptAddress(_connectionParams.Network);


            var unlockedDust = new TxOut(Money.Zero, unlockedAddress.ScriptPubKey).GetDustThreshold(builder.StandardTransactionPolicy.MinRelayTxFee);
            var lockedDust = new TxOut(Money.Zero, lockedAddress.ScriptPubKey).GetDustThreshold(builder.StandardTransactionPolicy.MinRelayTxFee);

            if (OpenAssetsHelper.IsBitcoin(asset.Id))
            {
                if (unlockedAmount < unlockedDust.ToDecimal(MoneyUnit.BTC))
                {
                    lockedAmount += unlockedAmount;
                    unlockedAmount = 0;
                }

                if (lockedAmount < lockedDust.ToDecimal(MoneyUnit.BTC))
                {
                    unlockedAmount += lockedAmount;
                    lockedAmount = 0;
                }

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
                    dustAmount += unlockedDust;
                }
                if (lockedAmount > 0)
                {
                    builder.SendAsset(lockedAddress, new AssetMoney(assetId, lockedAmount, asset.MultiplierPower));
                    dustAmount += lockedDust;
                }
                additionalBtc = dustAmount - sendAmount;
            }

            var fakeFee = new Money(1, MoneyUnit.BTC);

            var fakeAmount = additionalBtc + fakeFee;
            builder.SendFees(fakeFee);

            _transactionBuildHelper.AddFakeInput(builder, fakeAmount);
            var tr = builder.BuildTransaction(true);
            _transactionBuildHelper.RemoveFakeInput(tr);
            return new CreationCommitmentResult(tr, lockedAddress.ToString(), script.ToHex());
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
                var channel = await _offchainChannelRepository.GetLastChannel(multisig, asset.Id);
                if (channel != null)
                {
                    string hash = null;
                    if (channel.IsBroadcasted)
                        hash = new Transaction(channel.FullySignedChannel).GetHash().ToString();
                    result.Channels[asset.Id] = new OffchainBalanceInfo
                    {
                        ClientAmount = channel.ClientAmount,
                        HubAmount = channel.HubAmount,
                        TransactionHash = hash,
                        Actual = channel.Actual
                    };
                }
            }
            return result;
        }

        public async Task<IAssetSetting> GetAssetSetting(string asset)
        {
            var setting = await _assetSettingCache.GetItemAsync(asset) ??
                          await _assetSettingCache.GetItemAsync(Constants.DefaultAssetSetting);
            if (setting == null)
                throw new BackendException($"Asset setting is not found for {asset}", ErrorCode.AssetSettingNotFound);
            return setting;
        }

        public async Task<IEnumerable<IOffchainChannel>> GetCurrentChannels(string multisig)
        {
            return await _offchainChannelRepository.GetChannelsOfMultisig(multisig);
        }

        public async Task<IEnumerable<CommitmentBroadcastInfo>> GetCommitmentBroadcasts(int limit)
        {
            return (await _commitmentBroadcastRepository.GetLastCommitmentBroadcasts(limit)).Select(o => new CommitmentBroadcastInfo(o))
                .ToList();
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

﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Bitcoin.Api.Client.AutoGenerated.Models;
using Lykke.Bitcoin.Api.Client.BitcoinApi.Models;
using Microsoft.Rest;
using OffchainClientBalanceResponse = Lykke.Bitcoin.Api.Client.BitcoinApi.Models.OffchainClientBalanceResponse;

namespace Lykke.Bitcoin.Api.Client.BitcoinApi
{
    public interface IBitcoinApiClient
    {
        //onchain
        Task<OnchainResponse> IssueAsync(IssueData data);
        Task<OnchainResponse> TransferAsync(TransferData data);
        Task<OnchainResponse> TransferAllAsync(TransferAllData data);
        Task<OnchainResponse> DestroyAsync(DestroyData data);
        Task<OnchainResponse> SwapAsyncTransaction(SwapData data);
        Task<OnchainResponse> RetryAsync(RetryData data);

        //offchain
        Task<OffchainResponse> OffchainTransferAsync(OffchainTransferData data);
        Task<OffchainClosingResponse> CreateChannelAsync(CreateChannelData data);
        Task<OffchainResponse> CreateHubCommitment(CreateHubComitmentData data);
        Task<OffchainResponse> Finalize(FinalizeData data);
        Task<OffchainClosingResponse> Cashout(CashoutData data);
        Task<OffchainBaseResponse> CloseChannel(CloseChannelData data);
        Task<OffchainClosingResponse> HubCashout(HubCashoutData data);
        Task<OffchainBalancesResponse> Balances(string multisig);
        Task<OffchainAssetBalancesResponse> ChannelsInfo(string asset, DateTime? date);
        Task<OffchainBaseResponse> BroadcastCommitment(BroadcastCommitmentData data);
        Task<OffchainBaseResponse> BroadcastLastCommitment(string multisig, string asset);
        Task<OffchainClientBalanceResponse> GetClientBalance(string multisig, string asset);
        Task<MultisigChannelsResponse> GetChannels(string multisig, string asset);
        Task<OffchainCommitmentsResponse> GetCommitments(Guid channelId);
        Task<TransactionHexResponse> GetCommitment(Guid commitmentId);
        Task<Response> RemoveChannelAsync(string multisig, string asset);
        Task<CommitmentBroadcastsResponse> GetCommitmentBroadcasts(int limit);

        //bcc
        Task<BccSplitTransactionResponse> BccSplitTransaction(string multisig, string clientDestination, string hubDestination);
        Task<BccTransactionResponse> BccPrivateTransferTransaction(string sourceAddress, string destinationAddress, decimal fee);
        Task<BccTransactionHashResponse> BccBroadcast(string transaction);
        Task<Models.BccBalanceResponse> BccBalance(string address);

        //isAlive
        Task<HttpOperationResponse> IsAlive();
        Task<HttpOperationResponse> IsAliveRpc();
        Task<HttpOperationResponse> IsAliveNinja();

        //transaction
        Task<OnchainResponse> TransactionTransfer(Guid? transactionId, string sourceAddress, string destinationAddress, decimal amount, string asset);
        Task TransactionBroadcast(Guid transactionId, string transaction);
        Task<OnchainResponse> TransactionMultipleTransfer(Guid? transactionId, string destination, string asset, int feeRate, decimal fixedFee,
            IEnumerable<ToOneAddress> sources);
        Task<OnchainResponse> TransactionGetById(Guid transactionId);


        //wallet
        Task<AllWalletsResponse> GetAllWallets();
        Task<Wallet> GetWallet(string clientPubKey);
        Task<LykkePayWallet> GenerateLykkePayWallet();
    }
}

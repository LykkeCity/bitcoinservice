using System;
using System.Threading.Tasks;
using Core.BitCoin.BitcoinApi.Models;

namespace Core.BitCoin.BitcoinApi
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
        Task<OffchainAssetBalancesResponse> ChannelsInfo(string asset);

        //bcc
        Task<BccSplitTransactionResponse> BccSplitTransaction(string multisig, string clientDestination, string hubDestination);
        Task<BccTransactionResponse> BccPrivateTransferTransaction(string sourceAddress, string destinationAddress, decimal fee);
        Task<BccTransactionHashResponse> BccBroadcast(string transaction);
    }
}

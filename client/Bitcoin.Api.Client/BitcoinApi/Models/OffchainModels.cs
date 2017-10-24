using System;
using System.Collections.Generic;

namespace Bitcoin.Api.Client.BitcoinApi.Models
{
    public class OffchainTransferData
    {
        public string ClientPubKey { get; set; }
        public decimal Amount { get; set; }
        public string AssetId { get; set; }
        public string ClientPrevPrivateKey { get; set; }
        public string ExternalTransferId { get; set; }
        public bool Required { get; set; }
    }

    public class CreateChannelData
    {
        public string ClientPubKey { get; set; }
        public decimal ClientAmount { get; set; }
        public decimal HubAmount { get; set; }
        public string AssetId { get; set; }
        public string ExternalTransferId { get; set; }
        public bool Required { get; set; }
    }

    public class CreateHubComitmentData
    {
        public string ClientPubKey { get; set; }
        public decimal Amount { get; set; }
        public string AssetId { get; set; }
        public string SignedByClientChannel { get; set; }
    }

    public class FinalizeData
    {
        public string ClientPubKey { get; set; }
        public string AssetId { get; set; }
        public string ClientRevokePubKey { get; set; }
        public string SignedByClientHubCommitment { get; set; }
        public string ExternalTransferId { get; set; }
        public string OffchainTransferId { get; set; }
    }

    public class CashoutData
    {
        public string ClientPubKey { get; set; }
        public string HotWalletAddress { get; set; }
        public string CashoutAddress { get; set; }
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
    }

    public class CloseChannelData
    {
        public string ClientPubKey { get; set; }
        public string AssetId { get; set; }
        public string SignedClosingTransaction { get; set; }
        public string OffchainTransferId { get; set; }
    }

    public class HubCashoutData
    {
        public string ClientPubKey { get; set; }
        public string Hotwallet { get; set; }
        public string AssetId { get; set; }
    }

    public class BroadcastCommitmentData
    {
        public string ClientPubKey { get; set; }

        public string Asset { get; set; }

        public string Transaction { get; set; }
    }

    public class OffchainBaseResponse : Response
    {
        public string TxHash { get; set; }
    }

    public class OffchainResponse : OffchainBaseResponse
    {
        public string Transaction { get; set; }
        public Guid? TransferId { get; set; }
    }

    public class OffchainClosingResponse : OffchainResponse
    {
        public bool ChannelClosing { get; set; }
    }

    public class OffchainBalancesResponse : OffchainBaseResponse
    {
        public Dictionary<string, OffchainChannelBalance> Balances { get; set; }
    }

    public class OffchainAssetBalancesResponse : OffchainBaseResponse
    {
        public IEnumerable<MultisigBalance> Balances { get; set; }
    }

    public class MultisigChannelsResponse : Response
    {
        public IEnumerable<OffchainChannelInfo> Channels { get; set; }
    }

    public class OffchainClientBalanceResponse : Response
    {
        public decimal Amount { get; set; }
    }

    public class OffchainCommitmentsResponse : Response
    {
        public List<OffchainCommitment> Commitments { get; set; }
    }

    public class TransactionHexResponse : Response
    {
        public string Hex { get; set; }
    }

    public class CommitmentBroadcastsResponse : Response
    {
        public List<CommitmentBroadcast> Broadcasts { get; set; }
    }

    public class OffchainChannelBalance
    {
        public decimal ClientAmount { get; set; }
        public decimal HubAmount { get; set; }
        public string Hash { get; set; }
        public bool Actual { get; set; }
    }

    public class MultisigBalance
    {
        public decimal ClientAmount { get; set; }
        public decimal HubAmount { get; set; }
        public string Multisig { get; set; }

        public DateTime UpdateDt { get; set; }
    }


    public class OffchainChannelInfo : OffchainChannelBalance
    {
        public Guid ChannelId { get; set; }

        public DateTime Date { get; set; }
    }


    public class OffchainCommitment
    {
        public DateTime Date { get; set; }

        public decimal ClientAmount { get; set; }

        public decimal HubAmount { get; set; }

        public Guid ClientCommitment { get; set; }

        public Guid HubCommitment { get; set; }
    }



    public class CommitmentBroadcast
    {
        public Guid CommitmentId { get; set; }
        public string TransactionHash { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public decimal ClientAmount { get; set; }
        public decimal HubAmount { get; set; }
        public decimal RealClientAmount { get; set; }
        public decimal RealHubAmount { get; set; }
        public string PenaltyTransactionHash { get; set; }
    }
}

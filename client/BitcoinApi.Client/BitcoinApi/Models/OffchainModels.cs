using System;
using System.Collections.Generic;
using Bitcoin.Api.Client.BitcoinApi.Models;

namespace Core.BitCoin.BitcoinApi.Models
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
        public IEnumerable<OffchainChannelBalance> Balances { get; set; }
    }

    public class OffchainChannelBalance
    {
        public string Multisig { get; set; }
        public decimal ClientAmount { get; set; }
        public decimal HubAmount { get; set; }
        public string Hash { get; set; }
        public bool Actual { get; set; }
        public DateTime UpdateDt { get; set; }
    }
}

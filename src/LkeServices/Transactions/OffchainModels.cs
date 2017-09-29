using System;
using System.Collections.Generic;
using System.Text;
using Core.Repositories.Offchain;

namespace LkeServices.Transactions
{
    public class OffchainResponse
    {
        public Guid TransferId { get; set; }

        public string TransactionHex { get; set; }

        public bool ChannelClosed { get; set; }
    }


    public class OffchainFinalizeResponse : OffchainResponse
    {
        public string Hash { get; set; }
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
        public bool Actual { get; set; }
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

    public class MultisigBalanceInfo
    {
        public string Multisig { get; set; }

        public decimal ClientAmount { get; set; }

        public decimal HubAmount { get; set; }
    }

    public class AssetBalanceInfo
    {
        public List<MultisigBalanceInfo> Balances { get; set; }
    }

    public class CommitmentBroadcastInfo
    {
        public Guid CommitmentId { get; set; }
        public string TransactionHash { get; set; }
        public DateTime Date { get; set; }
        public CommitmentBroadcastType Type { get; set; }
        public decimal ClientAmount { get; set; }
        public decimal HubAmount { get; set; }
        public decimal RealClientAmount { get; set; }
        public decimal RealHubAmount { get; set; }
        public string PenaltyTransactionHash { get; set; }


        public CommitmentBroadcastInfo(ICommitmentBroadcast commitmentBroadcast)
        {
            CommitmentId = commitmentBroadcast.CommitmentId;
            TransactionHash = commitmentBroadcast.TransactionHash;
            Date = commitmentBroadcast.Date;
            Type = commitmentBroadcast.Type;
            ClientAmount = commitmentBroadcast.ClientAmount;
            HubAmount = commitmentBroadcast.HubAmount;
            RealClientAmount = commitmentBroadcast.RealClientAmount;
            RealHubAmount = commitmentBroadcast.RealHubAmount;
            PenaltyTransactionHash = commitmentBroadcast.PenaltyTransactionHash;
        }
    }
}

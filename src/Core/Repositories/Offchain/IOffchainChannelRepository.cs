using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using NBitcoin;

namespace Core.Repositories.Offchain
{
    public interface IOffchainChannel
    {
        Guid TransactionId { get; }

        string Multisig { get; }

        string Asset { get;}

        string InitialTransaction { get; }
        
        decimal ClientAmount { get;} 

        decimal HubAmount { get; }

        string FullySignedChannel { get; }
    }


    public interface IOffchainChannelRepository
    {
        Task<IOffchainChannel> CreateChannel(Guid transactionId, string multisig, string asset, string initialTr);

        Task<IOffchainChannel> GetChannel(string multisig, string assetName);
        Task SetFullSignedTransactionAndAmount(string multisig, string assetName, string fullSignedTr, decimal hubAmount, decimal clientAmount);
    }
}

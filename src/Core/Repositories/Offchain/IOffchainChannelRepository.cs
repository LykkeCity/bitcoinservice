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
        Guid ChannelId { get; }

        string Multisig { get; }

        string Asset { get;}

        string InitialTransaction { get; }
        
        decimal ClientAmount { get;} 

        decimal HubAmount { get; }

        string FullySignedChannel { get; }

        bool IsBroadcasted { get; }

        DateTime CreateDt { get; }

        Guid? PrevChannelTransactionId { get; set; }        
    }


    public interface IOffchainChannelRepository
    {
        Task<IOffchainChannel> CreateChannel(string multisig, string asset, string initialTr, decimal clientAmount, decimal hubAmount);

        Task<IOffchainChannel> GetChannel(string multisig, string asset);

        Task<IEnumerable<Offchain.IOffchainChannel>> GetChannels(string multisig, string asett);

        Task<IOffchainChannel> SetFullSignedTransaction(string multisig, string asset, string fullSignedTr);

        Task UpdateAmounts(string multisig, string asset, decimal clientAmount, decimal hubAmount);

        Task SetChannelBroadcasted(string multisig, string asset);

        Task CloseChannel(string multisig, string asset, Guid channelId);
        Task RevertChannel(string multisig, string asset, Guid channelId);
        Task<bool> HasChannel(string multisig);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.Offchain
{
    public interface IClosingChannel
    {
        Guid ClosingChannelId { get; }

        string Multisig { get; }

        string Asset { get; }

        string InitialTransaction { get; }

        Guid ChannelId { get; }
    }


    public interface IClosingChannelRepository
    {
        Task<IClosingChannel> GetClosingChannel(string multisig, string asset);
        Task<IClosingChannel> CreateClosingChannel(string multisig, string asset, Guid channelId, string initialTransaction);
        Task CompleteClosingChannel(string multisig, string asset, Guid closingChannelId);
    }
}

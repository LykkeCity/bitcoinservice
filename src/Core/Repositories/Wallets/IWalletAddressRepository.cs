using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.Wallets
{
    public interface IWalletAddress
    {
        string ClientPubKey { get; }
        string ExchangePubKey { get; }
        string MultisigAddress { get; }
        string RedeemScript { get; }
    }

    public interface IWalletAddressRepository
    {
        Task<IWalletAddress> Create(string multisig, string clientPubKey, string exchangePubKey, string redeemScript);
        Task<string> GetRedeemScript(string multisigAdress);
        Task<IWalletAddress> GetByClientPubKey(string clientPubKey);        
    }
}

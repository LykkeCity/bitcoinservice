using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories.Wallets
{

    public interface ISegwitPrivateWallet
    {
        string ClientPubKey { get; set; }

        string Address { get; }
        string Redeem { get; set; }
        string SegwitPubKey { get; set; }
    }

    public interface ISegwitPrivateWalletRepository
    {
        Task<ISegwitPrivateWallet> AddSegwitPrivateWallet(string clientPubKey, string address, string segwitPubKey, string redeem);
        Task<ISegwitPrivateWallet> GetSegwitPrivateWallet(string address);
        Task<ISegwitPrivateWallet> GetByClientPubKey(string clientPubKey);
        Task<string> GetRedeemScript(string address);
    }
}

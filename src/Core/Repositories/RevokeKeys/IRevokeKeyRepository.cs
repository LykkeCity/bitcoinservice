using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.RevokeKeys
{
    public enum RevokeKeyType
    {
        Exchange,
        Client
    }

    public interface IRevokeKey
    {
        string PubKey { get; }
        string PrivateKey { get; }
        RevokeKeyType Type { get; }
    }

    public interface IRevokeKeyRepository
    {    
        Task<IRevokeKey> GetRevokeKey(string pubKey);
        Task AddRevokeKey(string pubkey, RevokeKeyType type, string privateKey = null);
        Task AddPrivateKey(string pubkey, string privateKey);
    }
}

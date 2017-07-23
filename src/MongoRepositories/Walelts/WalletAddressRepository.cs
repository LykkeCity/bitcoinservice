using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Repositories.Wallets;
using MongoRepositories.Mongo;

namespace MongoRepositories.Walelts
{
    public class WalletAddressEntity : MongoEntity, IWalletAddress
    {
        public string ClientPubKey { get; set; }
        public string ExchangePubKey { get; set; }
        public string MultisigAddress { get; set; }
        public string RedeemScript { get; set; }



        public static WalletAddressEntity Create(string multisig, string clientPubKey, string exchangePubKey, string redeemScript)
        {
            return new WalletAddressEntity
            {
                BsonId = multisig,
                MultisigAddress = multisig,
                RedeemScript = redeemScript,
                ClientPubKey = clientPubKey,
                ExchangePubKey = exchangePubKey
            };
        }



    }

    public class WalletAddressRepository : IWalletAddressRepository
    {
        private readonly IMongoStorage<WalletAddressEntity> _storage;

        public WalletAddressRepository(IMongoStorage<WalletAddressEntity> storage)
        {
            _storage = storage;
        }

        public async Task<IWalletAddress> Create(string multisig, string clientPubKey, string exchangePubKey, string redeemScript)
        {
            var rec = WalletAddressEntity.Create(multisig, clientPubKey, exchangePubKey, redeemScript);
            await _storage.InsertAsync(rec);
            return rec;
        }

        public async Task<string> GetRedeemScript(string multisigAdress)
        {
            var data = await _storage.GetDataAsync(multisigAdress);
            return data.RedeemScript;
        }

        public async Task<IWalletAddress> GetByClientPubKey(string clientPubKey)
        {
            return (await _storage.GetDataAsync(o => o.ClientPubKey == clientPubKey)).FirstOrDefault();
        }

        public async Task<IEnumerable<IWalletAddress>> GetAllAddresses()
        {
            return await _storage.GetDataAsync();
        }

        public async Task<IWalletAddress> GetByMultisig(string multisig)
        {
            return (await _storage.GetDataAsync(o => o.MultisigAddress == multisig)).FirstOrDefault();
        }
    }
}

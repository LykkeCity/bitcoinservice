using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Repositories.Wallets;
using MongoRepositories.Mongo;

namespace MongoRepositories.Walelts
{
    public class SegwitPrivateWalletEntity : MongoEntity, ISegwitPrivateWallet
    {
        public string Address => BsonId;
        public string Redeem { get; set; }
        public string SegwitPubKey { get; set; }
        public string ClientPubKey { get; set; }        

        public static SegwitPrivateWalletEntity Create(string clientPubKey, string address, string segwitPubKey, string redeem)
        {
            return new SegwitPrivateWalletEntity
            {
                BsonId = address,
                SegwitPubKey = segwitPubKey,
                ClientPubKey = clientPubKey,
                Redeem = redeem
            };
        }
    }



    public class SegwitPrivateWalletRepository : ISegwitPrivateWalletRepository
    {
        private readonly IMongoStorage<SegwitPrivateWalletEntity> _table;

        public SegwitPrivateWalletRepository(IMongoStorage<SegwitPrivateWalletEntity> table)
        {
            _table = table;
        }


        public async Task<ISegwitPrivateWallet> AddSegwitPrivateWallet(string clientPubKey, string address, string segwitPubKey, string redeem)
        {
            var entity = SegwitPrivateWalletEntity.Create(clientPubKey, address, segwitPubKey, redeem);
            await _table.InsertAsync(entity);
            return entity;
        }        

        public async Task<ISegwitPrivateWallet> GetSegwitPrivateWallet(string address)
        {
            return await _table.GetDataAsync(address);
        }

        public async Task<ISegwitPrivateWallet> GetByClientPubKey(string clientPubKey)
        {
            return (await _table.GetDataAsync(o => o.ClientPubKey == clientPubKey)).FirstOrDefault();
        }

        public async Task<string> GetRedeemScript(string address)
        {
            var addr = await GetSegwitPrivateWallet(address);
            return addr?.Redeem;
        }
    }
}

using System;
using System.Threading.Tasks;
using Core.Repositories.RevokeKeys;
using MongoDB.Bson.Serialization.Attributes;
using MongoRepositories.Mongo;

namespace MongoRepositories.RevokeKeys
{


    public class RevokeKeyEntity : MongoEntity, IRevokeKey
    {
        [BsonIgnore]
        public string PubKey => BsonId;
        public string PrivateKey { get; set; }
        public RevokeKeyType Type { get; set; }

        public static RevokeKeyEntity Create(string pubKey, RevokeKeyType type, string privateKey)
        {
            return new RevokeKeyEntity
            {
                BsonId = pubKey,
                Type = type,
                PrivateKey = privateKey
            };
        }
    }


    public class RevokeKeyRepository : IRevokeKeyRepository
    {
        private readonly IMongoStorage<RevokeKeyEntity> _table;

        public RevokeKeyRepository(IMongoStorage<RevokeKeyEntity> table)
        {
            _table = table;
        }


        public async Task<IRevokeKey> GetRevokeKey(string pubKey)
        {
            return await _table.GetDataAsync(pubKey);
        }

        public Task AddRevokeKey(string pubkey, RevokeKeyType type, string privateKey = null)
        {
            return _table.InsertAsync(RevokeKeyEntity.Create(pubkey, type, privateKey));
        }

        public Task AddPrivateKey(string pubkey, string privateKey)
        {
            return _table.ReplaceAsync(pubkey, entity =>
            {
                entity.PrivateKey = privateKey;
                return entity;
            });
        }
    }
}

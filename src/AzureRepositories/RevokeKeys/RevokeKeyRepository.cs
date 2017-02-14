using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.RevokeKeys;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.RevokeKeys
{


    public class RevokeKeyEntity : TableEntity, IRevokeKey
    {
        public string PubKey => RowKey;
        public string PrivateKey { get; set; }
        public RevokeKeyType Type => (RevokeKeyType)Enum.Parse(typeof(RevokeKeyType), RevokeType);

        public string RevokeType { get; set; }

        public static string GeneratePartition()
        {
            return "RevokeKey";
        }

        public static RevokeKeyEntity Create(string pubKey, RevokeKeyType type, string privateKey)
        {
            return new RevokeKeyEntity
            {
                RowKey = pubKey,
                PartitionKey = GeneratePartition(),
                RevokeType = type.ToString(),
                PrivateKey = privateKey
            };
        }
    }


    public class RevokeKeyRepository : IRevokeKeyRepository
    {
        private readonly INoSQLTableStorage<RevokeKeyEntity> _table;

        public RevokeKeyRepository(INoSQLTableStorage<RevokeKeyEntity> table)
        {
            _table = table;
        }


        public async Task<IRevokeKey> GetRevokeKey(string pubKey)
        {
            return await _table.GetDataAsync(RevokeKeyEntity.GeneratePartition(), pubKey);
        }

        public Task AddRevokeKey(string pubkey, RevokeKeyType type, string privateKey = null)
        {
            return _table.InsertAsync(RevokeKeyEntity.Create(pubkey, type, privateKey));
        }

        public Task AddPrivateKey(string pubkey, string privateKey)
        {
            return _table.ReplaceAsync(RevokeKeyEntity.GeneratePartition(), pubkey, entity =>
            {
                entity.PrivateKey = privateKey;
                return entity;
            });
        }
    }
}

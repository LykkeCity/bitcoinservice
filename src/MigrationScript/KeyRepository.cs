using System.Threading.Tasks;
using AzureStorage;
using Microsoft.WindowsAzure.Storage.Table;
using NBitcoin;

namespace MigrationScript
{
    public interface IKeyRepository
    {
        Task<string> GetPrivateKey(string address);
        Task CreatePrivateKey(string address, string privateKey);
    }

    public interface IKeyData
    {
        string Address { get; }
        string PrivateKey { get; }
    }

    public class KeyEntity : TableEntity, IKeyData
    {
        public static string GeneratePartitionKey()
        {
            return "KeyPairs";
        }

        public string Address => RowKey;
        public string PrivateKey { get; set; }

        public static KeyEntity Create(string address, string privateKey)
        {
            return new KeyEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = address,
                PrivateKey = privateKey
            };
        }
    }

    public class KeyRepository : IKeyRepository
    {
        private readonly INoSQLTableStorage<KeyEntity> _storage;

        public KeyRepository(INoSQLTableStorage<KeyEntity> storage)
        {
            _storage = storage;
        }

        public async Task<string> GetPrivateKey(string address)
        {
            var entity = await _storage.GetDataAsync(KeyEntity.GeneratePartitionKey(), address);

            return entity?.PrivateKey;
        }

        public Task CreatePrivateKey(string address, string privateKey)
        {
            return _storage.InsertAsync(KeyEntity.Create(address, privateKey));
        }
    }
}

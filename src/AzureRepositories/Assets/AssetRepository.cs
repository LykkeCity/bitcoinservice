using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Repositories.Assets;
using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage;
using Core.OpenAssets;

namespace AzureRepositories.Assets
{
    public class AssetEntity : TableEntity, IAsset
    {
        public static string GeneratePartitionKey()
        {
            return "Asset";
        }

        public string Id => RowKey;
        public string BlockChainAssetId { get; set; }
        public string AssetAddress { get; set; }
        public int MultiplierPower { get; set; }
        public string DefinitionUrl { get; set; }
        public bool IsDisabled { get; set; }
        public string PartnerId { get; set; }
        public bool IssueAllowed { get; set; }
        public bool NotLykkeAsset { get; set; }
        public double LowVolumeAmount { get; set; }

        public string Blockchain { get; set; }
    }

    public class AssetRepository : IAssetRepository
    {
        private readonly INoSQLTableStorage<AssetEntity> _storage;

        public AssetRepository(INoSQLTableStorage<AssetEntity> storage)
        {
            _storage = storage;
        }

        public async Task<IAsset> GetAssetById(string id)
        {
            return await _storage.GetDataAsync(AssetEntity.GeneratePartitionKey(), id);
        }

        public async Task<IEnumerable<IAsset>> GetBitcoinAssets()
        {
            return await _storage.GetDataAsync(AssetEntity.GeneratePartitionKey(),
                // NOTE: It's needed to specify BTC explicitly here since we've switched 'Blockchain'
                // field of BTC to 'Ethereum' to make it work on Sirius integration.
                entity => entity.Blockchain == "Bitcoin" || OpenAssetsHelper.IsBitcoin(entity.Id));
        }
    }
}

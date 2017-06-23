using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.Assets;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Assets
{
    public class AssetSettingEntity : BaseEntity, IAssetSetting
    {
        public static string GeneratePartitionKey()
        {
            return "Asset";
        }

        public string Asset => RowKey;
        public string HotWallet { get; set; }
        public decimal CashinCoef { get; set; }
        public decimal Dust { get; set; }
        public int MaxOutputsCountInTx { get; set; }
        public decimal MinBalance { get; set; }
        public decimal OutputSize { get; set; }
        public int MinOutputsCount { get; set; }
        public int MaxOutputsCount { get; set; }
        public string ChangeWallet { get; set; }

        public static class Archive
        {
            public static string GeneratePartition(string asset)
            {
                return "Archive_" + asset;
            }

            public static AssetSettingEntity Create(IAssetSetting setting)
            {
                return new AssetSettingEntity
                {
                    PartitionKey = GeneratePartition(setting.Asset),
                    RowKey = Guid.NewGuid().ToString(),
                    HotWallet = setting.HotWallet,
                    ChangeWallet = setting.ChangeWallet
                };
            }
        }
    }



    public class AssetSettingRepository : IAssetSettingRepository
    {
        private readonly INoSQLTableStorage<AssetSettingEntity> _table;

        public AssetSettingRepository(INoSQLTableStorage<AssetSettingEntity> table)
        {
            _table = table;
        }

        public async Task<IEnumerable<IAssetSetting>> GetAssetSettings()
        {
            return await _table.GetDataAsync(AssetSettingEntity.GeneratePartitionKey());
        }

        public async Task UpdateAssetSetting(string asset, string hotWallet, string changeWallet)
        {
            var entity = await _table.GetDataAsync(AssetSettingEntity.GeneratePartitionKey(), asset);
            if (entity != null)
            {
                var archive = AssetSettingEntity.Archive.Create(entity);
                await _table.InsertAsync(archive);
                await _table.ReplaceAsync(AssetSettingEntity.GeneratePartitionKey(), asset, updateEntity =>
                {
                    updateEntity.HotWallet = hotWallet;
                    updateEntity.ChangeWallet = changeWallet;
                    return updateEntity;
                });
            }
        }
    }
}

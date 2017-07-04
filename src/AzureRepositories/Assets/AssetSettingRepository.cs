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
        public int PrivateIncrement { get; set; }

        public IAssetSetting Clone(string newId)
        {
            return new AssetSettingEntity
            {
                PartitionKey = PartitionKey,
                RowKey = newId,
                HotWallet = HotWallet,
                CashinCoef = CashinCoef,
                Dust = Dust,
                MaxOutputsCountInTx = MaxOutputsCountInTx,
                MinBalance = MinBalance,
                OutputSize = OutputSize,
                MinOutputsCount = MinOutputsCount,
                MaxOutputsCount = MaxOutputsCount,
                ChangeWallet = ChangeWallet,
                PrivateIncrement = PrivateIncrement
            };
        }

        public static class ById
        {
            public static string GeneratePartition()
            {
                return "Asset";
            }

            public static AssetSettingEntity Create(IAssetSetting setting)
            {
                return new AssetSettingEntity
                {
                    PartitionKey = GeneratePartition(),
                    RowKey = setting.Asset,
                    HotWallet = setting.HotWallet,
                    CashinCoef = setting.CashinCoef,
                    Dust = setting.Dust,
                    MaxOutputsCountInTx = setting.MaxOutputsCountInTx,
                    MinBalance = setting.MinBalance,
                    OutputSize = setting.OutputSize,
                    MinOutputsCount = setting.MinOutputsCount,
                    MaxOutputsCount = setting.MaxOutputsCount,
                    ChangeWallet = setting.ChangeWallet,
                    PrivateIncrement = setting.PrivateIncrement
                };
            }
        }

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
            return await _table.GetDataAsync(AssetSettingEntity.ById.GeneratePartition());
        }

        public Task Insert(IAssetSetting setting)
        {
            return _table.InsertAsync(AssetSettingEntity.ById.Create(setting));
        }

        public async Task UpdateHotWallet(string asset, string hotWallet)
        {
            var entity = await _table.GetDataAsync(AssetSettingEntity.ById.GeneratePartition(), asset);
            if (entity != null)
            {
                var archive = AssetSettingEntity.Archive.Create(entity);
                await _table.InsertAsync(archive);
                await _table.ReplaceAsync(AssetSettingEntity.ById.GeneratePartition(), asset, updateEntity =>
                {
                    updateEntity.HotWallet = hotWallet;
                    return updateEntity;
                });
            }
        }

        public async Task UpdateChangeAndIncrement(string asset, string changeWallet, int increment)
        {
            var entity = await _table.GetDataAsync(AssetSettingEntity.ById.GeneratePartition(), asset);
            if (entity != null)
            {
                var archive = AssetSettingEntity.Archive.Create(entity);
                await _table.InsertAsync(archive);
                await _table.ReplaceAsync(AssetSettingEntity.ById.GeneratePartition(), asset, updateEntity =>
                {
                    updateEntity.ChangeWallet = changeWallet;
                    updateEntity.PrivateIncrement = increment;
                    return updateEntity;
                });
            }
        }

        public async Task<IAssetSetting> GetAssetSetting(string assetId)
        {
            return await _table.GetDataAsync(AssetSettingEntity.ById.GeneratePartition(), assetId);
        }
    }
}

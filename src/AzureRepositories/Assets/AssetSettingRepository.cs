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
    }
}

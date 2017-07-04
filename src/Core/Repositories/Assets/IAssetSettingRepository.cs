using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories.Assets
{
    public interface IAssetSetting
    {
        string Asset { get; }
        string HotWallet { get; set; }
        decimal CashinCoef { get; set; }
        decimal Dust { get; set; }
        int MaxOutputsCountInTx { get; set; }
        decimal MinBalance { get; set; }
        decimal OutputSize { get; set; }
        int MinOutputsCount { get; set; }
        int MaxOutputsCount { get; set; }
        string ChangeWallet { get; set; }
        int PrivateIncrement { get; set; }

        IAssetSetting Clone(string newId);
    }

    public interface IAssetSettingRepository
    {
        Task Insert(IAssetSetting setting);
        Task<IEnumerable<IAssetSetting>> GetAssetSettings();
        Task UpdateHotWallet(string asset, string hotWallet);
        Task UpdateChangeAndIncrement(string asset, string changeWallet, int increment);
        Task<IAssetSetting> GetAssetSetting(string assetId);
    }
}

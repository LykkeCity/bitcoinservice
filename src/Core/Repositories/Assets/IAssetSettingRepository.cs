using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories.Assets
{
    public interface IAssetSetting
    {        
        string Asset { get; set; }
        string HotWallet { get; set; }
        decimal CashinCoef { get; set; }
        decimal Dust { get; set; }
        int MaxOutputsCountInTx { get; set; }
        decimal MinBalance { get; set; }
        decimal OutputSize { get; set; }
        int MinOutputsCount { get; set; }
        int MaxOutputsCount { get; set; }
    }

    public interface IAssetSettingRepository
    {
        Task<IEnumerable<IAssetSetting>> GetAssetSettings();
    }
}

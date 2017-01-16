using System.ComponentModel;
using Core.Enums;
using Newtonsoft.Json;

namespace Core.Settings
{
    public class BaseSettings
    {
        public NetworkType NetworkType { get; set; }

        public string RPCUsername { get; set; }
        public string RPCPassword { get; set; }
        public string RPCServerIpAddress { get; set; }

        public string FeeAddress { get; set; }
        public FeeType21co FeeType { get; set; }
        public decimal FeeRateMultiplier { get; set; } = 1;

        public string QBitNinjaBaseUrl { get; set; }
        public string SignatureProviderUrl { get; set; }
        public string LykkeJobsUrl { get; set; }

        public DbSettings Db { get; set; }

        public bool UseLykkeApi { get; set; }

        public string ChangeAddress { get; set; }

        public string HotWalletForPregeneratedOutputs { get; set; }

        public int MinPregeneratedOutputsCount { get; set; } = 2;
        public int GenerateOutputsBatchSize { get; set; } = 4;

        public decimal PregeneratedFeeAmount { get; set; } = 0.001M;
        public decimal MinHotWalletBalance { get; set; } = 1;

        public int MinPregeneratedAssetOutputsCount { get; set; } = 50;
        public int GenerateAssetOutputsBatchSize { get; set; } = 100;

        public string[] IssuedAssets { get; set; } = new string[0];
    }

    public class DbSettings
    {
        public string LogsConnString { get; set; }
        public string InQueueConnString { get; set; }
        public string DataConnString { get; set; }
        public string DictsConnString { get; set; }
        public string SharedConnString { get; set; }
    }
}

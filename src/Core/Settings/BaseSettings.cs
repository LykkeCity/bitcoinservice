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
        public string ClientSignatureProviderUrl { get; set; }

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

        public int MaxDequeueCount { get; set; } = 1000;

        public int MaxQueueDelay { get; set; } = 5000;

        public int BroadcastMonitoringPeriodSeconds { get; set; } = 3600 * 2;

        public decimal SpendChangeFeeRateMultiplier { get; set; } = 0.2M;

        public int NumberOfChangeInputsForTransaction { get; set; } = 200;

        public int FeeReservePeriodSeconds { get; set; } = 5 * 60;

        public decimal MaxExtraAmount { get; set; } = 0.001M;

        public int ClientSignatureTimeoutSeconds { get; set; } = 0;

        public int RepeatNinjaCount { get; set; } = 3;

        public Offchain Offchain { get; set; } = new Offchain();
    }

    public class DbSettings
    {
        public string LogsConnString { get; set; }
        public string DataConnString { get; set; }
        public string MongoDataConnString { get; set; }
        public string DictsConnString { get; set; }
        public string SharedConnString { get; set; }
        public string ClientPersonalInfoConnString { get; set; }
        public string BackofficeConnString { get; set; }
        public string ClientSignatureConnString { get; set; }
    }

    public class Offchain
    {
        public bool UseOffchainGeneration { get; set; } = false;

        public string HotWallet { get; set; }

        public int MaxIssuedOutputsInTransaction { get; set; } = 5;
        public int MaxSplittedOutputsInTransaction { get; set; } = 100;

        public decimal IssueAllowedCoinOutputSize { get; set; } = 100000;
        public decimal MinIssueAllowedCoinBalance { get; set; } = 1000000;
        public decimal MaxIssueAllowedCoinBalance { get; set; } = 10000000;
       
        public decimal MinBtcBalance { get; set; } = 10;
        public decimal BtcOutpitSize { get; set; } = 0.5M;
        public int MaxCountOfBtcOutputs { get; set; } = 10;
        public int MinCountOfBtcOutputs { get; set; } = 5;

        public decimal MinLkkBalance { get; set; } = 50000;
        public decimal LkkOutputSize { get; set; } = 10000;
        public int MaxCountOfLkkOutputs { get; set; } = 10;
        public int MinCountOfLkkOutputs { get; set; } = 5;
        public int FiatAssetAmountCoef { get; set; } = 5;
    }
}

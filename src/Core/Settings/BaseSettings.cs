using Core.Enums;
using Lykke.SettingsReader.Attributes;

namespace Core.Settings
{
    public class BaseSettings
    {
        public NetworkType NetworkType { get; set; }

        public string RPCUsername { get; set; }

        public string RPCPassword { get; set; }

        public string RPCServerIpAddress { get; set; }

        public string FeeAddress { get; set; }

        [HttpCheck("/")]
        public string QBitNinjaBaseUrl { get; set; }

        [HttpCheck("/api/isalive")]
        public string SignatureProviderUrl { get; set; }

        [HttpCheck("/api/isalive")]
        public string ClientSignatureProviderUrl { get; set; }

        [HttpCheck("/api/isalive")]
        public string BitcoinCallbackUrl { get; set; }

        public DbSettings Db { get; set; }

        public bool UseLykkeApi { get; set; }

        public string ChangeAddress { get; set; }

        public string HotWalletForPregeneratedOutputs { get; set; }

        public Offchain Offchain { get; set; } = new Offchain();

        public BccSettings Bcc { get; set; } = new BccSettings();

        [Optional]
        public FeeType21co FeeType { get; set; }

        [Optional]
        public decimal FeeRateMultiplier { get; set; } = 1;

        [Optional]
        public int MinPregeneratedOutputsCount { get; set; } = 2;

        [Optional]
        public int GenerateOutputsBatchSize { get; set; } = 4;

        [Optional]
        public decimal PregeneratedFeeAmount { get; set; } = 0.001M;

        [Optional]
        public decimal MinHotWalletBalance { get; set; } = 1;

        [Optional]
        public int MinPregeneratedAssetOutputsCount { get; set; } = 50;

        [Optional]
        public int GenerateAssetOutputsBatchSize { get; set; } = 100;

        [Optional]
        public int MaxDequeueCount { get; set; } = 1000;

        [Optional]
        public int MaxQueueDelay { get; set; } = 5000;

        [Optional]
        public int BroadcastMonitoringPeriodSeconds { get; set; } = 3600 * 2;

        [Optional]
        public decimal SpendChangeFeeRateMultiplier { get; set; } = 0.2M;

        [Optional]
        public int NumberOfChangeInputsForTransaction { get; set; } = 200;

        [Optional]
        public int FeeReservePeriodSeconds { get; set; } = 5 * 60;

        [Optional]
        public decimal MaxExtraAmount { get; set; } = 0.001M;

        [Optional]
        public int ClientSignatureTimeoutSeconds { get; set; } = 0;

        [Optional]
        public int BroadcastedOutputsExpirationDays { get; set; } = 7;

        [Optional]
        public int SpentOutputsExpirationDays { get; set; } = 7;

        [Optional]
        public Rabbit RabbitMq { get; set; } = new Rabbit();       
    }

    public class DbSettings
    {
        public string LogsConnString { get; set; }
        public string DataConnString { get; set; }
        public string MongoDataConnString { get; set; }
        public string DictsConnString { get; set; }
        public string SharedConnString { get; set; }
    }

    public class Offchain
    {
        [Optional]
        public bool UseOffchainGeneration { get; set; } = false;

        public string HotWallet { get; set; }
    }

    public class Rabbit
    {
        public RabbitMqConnectionSettings ExplorerNotificationConnection { get; set; } = new RabbitMqConnectionSettings();
        public RabbitMqConnectionSettings MultisigNotificationConnection { get; set; } = new RabbitMqConnectionSettings();
    }

    public class BccSettings
    {
        public NetworkType NetworkType { get; set; }
        public string RPCUsername { get; set; }
        public string RPCPassword { get; set; }
        public string RPCServerIpAddress { get; set; }

        [Optional]
        public bool UseBccNinja { get; set; }

        [Optional]
        public string QBitNinjaBaseUrl { get; set; }
    }
}

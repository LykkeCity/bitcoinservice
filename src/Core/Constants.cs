using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core
{
    public class Constants
    {
        public const string InDataQueue = "indata";
        public const string PregeneratedFeePoolQueue = "pregenerated-fee-queue";
        public const string SignedTransactionsQueue = "signed-transactions";
        public const string EmailNotifierQueue = "emailsqueue";
        public const string SlackNotifierQueue = "slack-notifications";

        public const string TransactionCommandQueue = "transaction-commands";
        public const string BroadcastingQueue = "broadcasting-transactions";
        public const string BroadcastMonitoringQueue = "broadcast-monitoring";
        public const string FeeReserveMonitoringQueue = "fee-reserve-monitoring";

        public const string ClientSignedTransactionQueue = "client-signed-transactions";
        public const string ClientSignMonitoringQueue = "client-sign-monitoring";
        public const string TransactionsForClientSignatureQueue = "transaction-out";

        public const string ReturnBroadcatedOutputsQueue = "return-broadcasted-outputs";

        public const string SpendCommitmentOutputQueue = "spend-commitment-queue";
        public const string PaidFeesTasksQueue = "paid-fees-tasks";


        public const string ProcessingBlockSetting = "ProcessingBlockSetting";
        public const string CurrentPrivateIncrementSetting = "PrivateIncrement";
        public const string MaxOffchainTxCount = "MaxOffchainTxCount";
        public const string BccBlockSetting = "FirstBccBlock";


        public const string DefaultAssetSetting = "Default";


        public const string RabbitMqExplorerNotification = "onh.offchainnotifications";
        public const string RabbitMqMultisigNotification = "multisignotifications";

        public const string BccKey = "bcc";
        public const int BccBlock = 478558;

        public static readonly DateTime PrevBccBlockTime = new DateTime(2017, 08, 01, 13, 16, 14, DateTimeKind.Utc);

        public const string BccTransferQueue = "bcc-transfer";
        
    }
}

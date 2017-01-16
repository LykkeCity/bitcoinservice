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
    }
}

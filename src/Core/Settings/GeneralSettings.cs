using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Settings
{
    public class GeneralSettings
    {
        public BaseSettings BitcoinApi { get; set; }
        public BaseSettings BitcoinJobs { get; set; }

        public SlackNotifications SlackNotifications { get; set; }
    }

    public class SlackNotifications
    {
        public int ThrottlingLimitSeconds { get; set; }
        public AzureQueueItem AzureQueue { get; set; }

        public class AzureQueueItem
        {
            public string ConnectionString { get; set; }
            public string QueueName { get; set; }
        }
    }
}

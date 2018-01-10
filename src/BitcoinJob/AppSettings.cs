using Core.Settings;
using Lykke.AzureQueueIntegration;

namespace BitcoinJob
{
    public class AppSettings
    {
        public BaseSettings BitcoinJobs { get; set; }

        public SlackNotificationsSettings SlackNotifications { get; set; }
    }

    public class SlackNotificationsSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }
    }
}

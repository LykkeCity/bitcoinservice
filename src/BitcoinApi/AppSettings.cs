using Core.Settings;
using Lykke.AzureQueueIntegration;

namespace BitcoinApi
{
    public class AppSettings
    {
        public BaseSettings BitcoinApi { get; set; }

        public SlackNotificationsSettings SlackNotifications { get; set; }
    }

    public class SlackNotificationsSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }
    }
}

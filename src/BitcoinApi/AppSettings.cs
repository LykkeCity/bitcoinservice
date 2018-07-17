using Core.Settings;
using Lykke.AzureQueueIntegration;

namespace BitcoinApi
{
    public class AppSettings
    {
        public BaseSettings BitcoinService { get; set; }

        public SlackNotificationsSettings SlackNotifications { get; set; }
    }

    public class SlackNotificationsSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }
    }
}

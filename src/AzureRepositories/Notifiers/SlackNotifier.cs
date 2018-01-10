using System;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Core;
using Core.Notifiers;
using Lykke.JobTriggers.Abstractions;
using Lykke.SlackNotifications;

namespace AzureRepositories.Notifiers
{
	public class SlackNotifier : ISlackNotifier, IPoisionQueueNotifier
	{
	    private readonly ISlackNotificationsSender _slackNotificationsSender;

		public SlackNotifier(ISlackNotificationsSender slackNotificationsSender)
		{
		    _slackNotificationsSender = slackNotificationsSender;
		}

		public async Task WarningAsync(string message)
		{
		    await _slackNotificationsSender.SendWarningAsync(message, "bitcoin service");
		}

        public async Task ErrorAsync(string message)
        {
            await _slackNotificationsSender.SendErrorAsync(message, "bitcoin service");
        }

        public async Task FinanceWarningAsync(string message)
        {
            await _slackNotificationsSender.SendAsync("Financewarnings", "bitcoin service", message);
        }

        public Task NotifyAsync(string message)
        {
            return WarningAsync(message);
        }
    }
}

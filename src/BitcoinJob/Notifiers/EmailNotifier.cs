using System;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Core;
using Newtonsoft.Json;

namespace BackgroundWorker.Notifiers
{
	public interface IEmailNotifier
	{
		Task WarningAsync(string title, string message);
	}

	public class EmailNotifier : IEmailNotifier
	{
		private readonly IQueueExt _queue;

		public EmailNotifier(Func<string, IQueueExt> queueFactory)
		{
			_queue = queueFactory(Constants.EmailNotifierQueue);
		}

		public async Task WarningAsync(string title, string message)
		{
			var obj = new
			{
				Data = new
				{
					BroadcastGroup = 100,
					MessageData = new
					{
						Subject = title,
						Text = message
					}
				}
			};

			var str = "PlainTextBroadcast:" + JsonConvert.SerializeObject(obj);

			await _queue.PutRawMessageAsync(str);
		}
	}
}

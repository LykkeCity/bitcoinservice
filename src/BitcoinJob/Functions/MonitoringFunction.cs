using System;
using System.Threading.Tasks;
using Core.Repositories.Monitoring;
using Lykke.JobTriggers.Triggers.Attributes;

namespace BitcoinJob.Functions
{
	public class MonitoringFunction
	{
		private readonly IMonitoringRepository _repository;

		public MonitoringFunction(IMonitoringRepository repository)
		{
			_repository = repository;
		}

        [TimerTrigger("00:00:30")]
		public async Task Execute()
		{
			await _repository.SaveAsync(new Monitoring { DateTime = DateTime.UtcNow, ServiceName = "BitcoinJobService",
				Version = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion});
		}
	}
}

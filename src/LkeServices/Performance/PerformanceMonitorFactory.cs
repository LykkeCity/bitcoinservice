using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core.Performance;

namespace LkeServices.Performance
{
    public class PerformanceMonitorFactory : IPerformanceMonitorFactory
    {
        private readonly ILog _logger;

        public PerformanceMonitorFactory(ILog logger)
        {
            _logger = logger;
        }

        public IPerformanceMonitor Create(string topProcess)
        {
            var monitor = new PerformanceMonitor(_logger);
            monitor.Start(topProcess);
            return monitor;
        }
    }
}

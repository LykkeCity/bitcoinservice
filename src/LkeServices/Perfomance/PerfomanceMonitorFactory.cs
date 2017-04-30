using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core.Perfomance;

namespace LkeServices.Perfomance
{
    public class PerfomanceMonitorFactory : IPerfomanceMonitorFactory
    {
        private readonly ILog _logger;

        public PerfomanceMonitorFactory(ILog logger)
        {
            _logger = logger;
        }

        public IPerfomanceMonitor Create(string topProcess)
        {
            var monitor = new PerfomanceMonitor(_logger);
            monitor.Start(topProcess);
            return monitor;
        }
    }
}

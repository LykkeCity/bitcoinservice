using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Performance
{
    public interface IPerformanceMonitorFactory
    {
        IPerformanceMonitor Create(string topProcess);
    }
}

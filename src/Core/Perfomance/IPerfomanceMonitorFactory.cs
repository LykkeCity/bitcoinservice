using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Perfomance
{
    public interface IPerfomanceMonitorFactory
    {
        IPerfomanceMonitor Create(string topProcess);
    }
}

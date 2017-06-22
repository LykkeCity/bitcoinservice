using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Performance
{
    public interface IPerformanceMonitor : IDisposable
    {       
        void Step(string nextStep);
        void ChildProcess(string childProcess);
        void Complete(string process);
        void CompleteLastProcess();
    }
}

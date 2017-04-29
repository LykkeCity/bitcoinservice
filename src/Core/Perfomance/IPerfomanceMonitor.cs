using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Perfomance
{
    public interface IPerfomanceMonitor : IDisposable
    {       
        void Step(string nextStep);
        void ChildProcess(string childProcess);
        void Complete(string process);
        void CompleteLastProcess();
    }
}

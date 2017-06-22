using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Performance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bitcoin.Tests
{
    [TestClass]
    public class PerfomanceMonitoringTests
    {
        [TestMethod]
        public void Test()
        {
            var factory = Config.Services.GetService<IPerformanceMonitorFactory>();

            IPerformanceMonitor monitoring;
            using (monitoring = factory.Create("Test")) 
            {
                Thread.Sleep(200);
                monitoring.Step("Step1");                
                monitoring.Step("Step2");
                Thread.Sleep(100);
                monitoring.ChildProcess("Child1");
                monitoring.Step("Step3");                
                monitoring.Step("Step4");
                Thread.Sleep(100);
                monitoring.ChildProcess("Child2");
                monitoring.Step("Step5");
                Thread.Sleep(200);
                monitoring.CompleteLastProcess();
                monitoring.Step("Step6");
                Thread.Sleep(200);
                monitoring.Complete("Child1");
                Thread.Sleep(200);
                monitoring.Step("Step7");
                Thread.Sleep(200);
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using Core.Performance;

namespace LkeServices.Performance
{
    public class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly ILog _log;

        private InternalMeasurer _topLevelMeasurer;

        private InternalMeasurer _currentMeasurer;

        public PerformanceMonitor(ILog log)
        {
            _log = log;
        }


        public void Dispose()
        {
            _topLevelMeasurer.Stop();
            _log.WriteInfoAsync("PerformanceMonitor", "Measure", null, _topLevelMeasurer.ToString());
        }

        internal void Start(string process)
        {
            _currentMeasurer = _topLevelMeasurer = new InternalMeasurer(process);
            _topLevelMeasurer.Start();
        }

        public void Step(string nextStep)
        {
            if (_currentMeasurer == null) return;
            var step = new InternalMeasurerStep(nextStep);
            _currentMeasurer.AddChild(step);
            step.Start();
        }

        public void ChildProcess(string childProcess)
        {
            if (_currentMeasurer == null) return;
            var child = new InternalMeasurer(childProcess);
            child.Start();
            _currentMeasurer.AddChild(child);
            _currentMeasurer = child;
        }

        public void Complete(string process)
        {
            var stopped = _topLevelMeasurer.Stop(process);
            _currentMeasurer = stopped.Parent;
        }

        public void CompleteLastProcess()
        {
            _currentMeasurer?.Stop();
            _currentMeasurer = _currentMeasurer?.Parent;
        }

        public override string ToString()
        {
            return _topLevelMeasurer.ToString();
        }

        private class InternalMeasurerStep : InternalMeasurer
        {
            internal override bool CanHoldChildren => false;

            public InternalMeasurerStep(string process) : base(process)
            {
            }
        }


        private class InternalMeasurer
        {
            private readonly string _process;
            private readonly List<InternalMeasurer> _children = new List<InternalMeasurer>();
            private readonly Stopwatch _stopwatch = new Stopwatch();

            internal virtual bool CanHoldChildren => true;
            public InternalMeasurer Parent { get; private set; }

            public InternalMeasurer(string process)
            {
                _process = process;
            }

            public void Start()
            {
                _stopwatch.Start();
            }

            public void Stop()
            {
                foreach (var internalMeasurer in _children)
                {
                    internalMeasurer.Stop();
                }
                if (_stopwatch.IsRunning)
                    _stopwatch.Stop();
            }


            public void AddChild(InternalMeasurer measurer)
            {
                if (!CanHoldChildren)
                    throw new Exception("Measurer can't hold child");
                _children.LastOrDefault()?.Stop();
                _children.Add(measurer);
                measurer.Parent = this;
            }

            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.AppendLine(_process + ": " + _stopwatch.ElapsedMilliseconds + "ms");
                foreach (var internalMeasurer in _children)
                {
                    var lines = internalMeasurer.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    builder.AppendLine(string.Join(Environment.NewLine, lines.Select(o => "   " + o)));
                }
                return builder.ToString();
            }

            internal InternalMeasurer Stop(string process)
            {
                if (_process == process)
                {
                    Stop();
                    return this;
                }
                else
                    return _children.Where(o => o.CanHoldChildren).Select(internalMeasurer => internalMeasurer.Stop(process)).FirstOrDefault(stopped => stopped != null);
            }
        }

    }
}

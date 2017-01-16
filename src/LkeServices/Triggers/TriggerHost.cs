using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LkeServices.Triggers.Bindings;

namespace LkeServices.Triggers
{
    public class TriggerHost
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly List<ITriggerBinding> _bindings = new List<ITriggerBinding>();

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private Assembly[] _assemblies;

        public TriggerHost(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void StartAndBlock()
        {
            var assemblies = CollectAssemblies().ToList();

            _bindings.AddRange(new TriggerBindingCollector<TimerTriggerBinding>().CollectFromAssemblies(assemblies, _serviceProvider));
            _bindings.AddRange(new TriggerBindingCollector<QueueTriggerBinding>().CollectFromAssemblies(assemblies, _serviceProvider));

            var tasks = _bindings.Select(o => o.RunAsync(_cancellationTokenSource.Token)).ToArray();
            Task.WaitAll(tasks);
        }

        public void ProvideAssembly(params Assembly[] assemblies)
        {
            _assemblies = assemblies;

        }

        private IEnumerable<Assembly> CollectAssemblies()
        {
            var list = new List<Assembly>() { Assembly.GetEntryAssembly() };
            if (_assemblies != null)
                list.AddRange(_assemblies);

            return list.GroupBy(x => x.FullName).Select(x => x.First());
        }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }


    }
}

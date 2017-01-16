using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LkeServices.Triggers.Bindings
{
    public interface ITriggerBinding
    {
	    void InitBinding(IServiceProvider serviceProvider, MethodInfo callbackMethod);

	    Task RunAsync(CancellationToken cancellationToken);
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using LkeServices.Triggers.Attributes;

namespace LkeServices.Triggers.Bindings
{
    [TriggerDefine(typeof(TimerTriggerAttribute))]
    public class TimerTriggerBinding : BaseTriggerBinding
    {
        private readonly ILog _log;
        private IServiceProvider _serviceProvider;
        private MethodInfo _method;

        private TimeSpan _period;
        private string _processId;

        public TimerTriggerBinding(ILog log)
        {
            if (log == null)
                throw new ArgumentNullException(nameof(log));
            _log = log;
        }

        public override void InitBinding(IServiceProvider serviceProvider, MethodInfo callbackMethod)
        {
            _serviceProvider = serviceProvider;
            _method = callbackMethod;
            var attribute = _method.GetCustomAttribute<TimerTriggerAttribute>();
            _period = attribute.Period;
            _processId = _method.DeclaringType.Name + "." + _method.Name;
            if (_method.GetParameters().Length > 0)
                throw new Exception($"Method {_method.Name} should be parameterless");
        }

        public override Task RunAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                        try
                        {
                            await Invoke(_serviceProvider, _method, null);
                        }
                        catch (Exception ex)
                        {
                            await LogError("TimerTriggerBinding", "RunAsync", ex);
                        }
                        finally
                        {                            
                            await Task.Delay(_period, cancellationToken);
                        }
                }
                finally
                {
                    await _log.WriteInfoAsync("TimerTriggerBinding", "RunAsync", _processId, "Process ended");
                }
            }, cancellationToken);
        }

        private Task LogError(string component, string process, Exception ex)
        {
            try
            {
                return _log.WriteErrorAsync(component, process, _processId, ex);
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Error in logger: {logEx.Message}. Trace: {logEx.StackTrace}");
                return Task.CompletedTask;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Common.PasswordTools;
using Core.Notifiers;
using Core.QueueReader;
using LkeServices.Triggers.Attributes;
using LkeServices.Triggers.Delay;

namespace LkeServices.Triggers.Bindings
{
    [TriggerDefine(typeof(QueueTriggerAttribute))]
    public class QueueTriggerBinding : BaseTriggerBinding
    {
        private readonly TimeSpan _minDelay = TimeSpan.FromMilliseconds(100);
        private readonly TimeSpan _maxDelay = TimeSpan.FromMinutes(1);
        private const int MaxDequeueCount = 5;
        private const string PoisonSuffix = "-poison";

        private readonly ILog _log;
        private readonly IQueueReaderFactory _queueReaderFactory;
        private readonly ISlackNotifier _slackNotifier;


        private IDelayStrategy _delayStrategy;
        private MethodInfo _method;
        private IServiceProvider _serviceProvider;
        private string _queueName;
        private IQueueReader _queueReader;
        private IQueueReader _poisonQueueReader;
        private Type _parameterType;
        private bool _hasSecondParameter;
        private bool _useTriggeringContext;
        private bool _shouldNotify;


        public QueueTriggerBinding(ILog log, IQueueReaderFactory queueReaderFactory, ISlackNotifier slackNotifier)
        {
            if (queueReaderFactory == null)
                throw new ArgumentNullException(nameof(queueReaderFactory));
            if (log == null)
                throw new ArgumentNullException(nameof(log));
            _log = log;
            _queueReaderFactory = queueReaderFactory;
            _slackNotifier = slackNotifier;
        }

        public override void InitBinding(IServiceProvider serviceProvider, MethodInfo callbackMethod)
        {
            _serviceProvider = serviceProvider;
            _method = callbackMethod;

            var metadata = _method.GetCustomAttribute<QueueTriggerAttribute>();

            _queueName = metadata.Queue;
            _queueReader = _queueReaderFactory.Create(_queueName);
            _shouldNotify = metadata.Notify;

            var parameters = _method.GetParameters();
            if (parameters.Length > 2 && parameters.Length < 1)
                throw new Exception($"Method {_method.Name} must have 1 or 2 parameters");
            if (parameters.Length == 2 && parameters[1].ParameterType != typeof(DateTimeOffset) && parameters[1].ParameterType != typeof(QueueTriggeringContext))
                throw new Exception($"Method {_method.Name} second parameter type is {parameters[1].ParameterType.Name}, but should by DateTimeOffset or QueueTriggeringContext");

            _parameterType = parameters[0].ParameterType;
            _hasSecondParameter = parameters.Length == 2;
            if (_hasSecondParameter)
                _useTriggeringContext = parameters[1].ParameterType == typeof(QueueTriggeringContext);
            _delayStrategy = new RandomizedExponentialStrategy(_minDelay,
                metadata.MaxPollingIntervalMs > 0 ? TimeSpan.FromMilliseconds(metadata.MaxPollingIntervalMs) : _maxDelay);
        }

        public override Task RunAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    IQueueMessage message = null;
                    bool executionSucceeded = false;
                    try
                    {
                        do
                        {
                            message = await _queueReader.GetMessageAsync();
                            if (message == null)
                                break;

                            var context = new QueueTriggeringContext(message.InsertionTime);

                            var p = new List<object>() { message.Value(_parameterType) };

                            if (_hasSecondParameter)
                                p.Add(_useTriggeringContext ? context : (object)message.InsertionTime);

                            await Invoke(_serviceProvider, _method, p.ToArray());
                            await ProcessCompletedMessage(message, context);
                            executionSucceeded = true;
                        } while (!cancellationToken.IsCancellationRequested);
                    }
                    catch (Exception ex)
                    {
                        await LogError("QueueTriggerBinding", "RunAsync", ex);
                        await ProcessFailedMessage(message);
                        executionSucceeded = false;                        
                    }
                    finally
                    {
                        await Task.Delay(_delayStrategy.GetNextDelay(executionSucceeded), cancellationToken);
                    }
                }
            }, cancellationToken);
        }



        private async Task ProcessCompletedMessage(IQueueMessage message, QueueTriggeringContext context)
        {
            switch (context.MovingAction)
            {
                case QueueTriggeringContext.MessageMovingAction.Default:
                    await _queueReader.FinishMessageAsync(message);
                    break;
                case QueueTriggeringContext.MessageMovingAction.MoveToEnd:
                    await MoveToEnd(message, context.NewMessageVersion);
                    break;
                case QueueTriggeringContext.MessageMovingAction.MoveToPoison:
                    await MoveToPoisonQueue(message, context.NewMessageVersion);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            await context.Delay(await _queueReader.Count());
        }

        private Task ProcessFailedMessage(IQueueMessage message)
        {
            try
            {
                if (message.DequeueCount >= MaxDequeueCount)
                    return MoveToPoisonQueue(message, null);
                else
                {
                    return _queueReader.ReleaseMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                return LogError("QueueTriggerBinding", "ProcessFailedMessage", ex);
            }
        }

        private async Task MoveToEnd(IQueueMessage message, string newMessageVersion)
        {
            newMessageVersion = newMessageVersion ?? message.AsString;
            await _queueReader.AddMessageAsync(newMessageVersion);
            await _queueReader.FinishMessageAsync(message);
        }

        private async Task MoveToPoisonQueue(IQueueMessage message, string newMessageVersion)
        {
            newMessageVersion = newMessageVersion ?? message.AsString;
            if (_poisonQueueReader == null)
                _poisonQueueReader = _queueReaderFactory.Create(_queueName + PoisonSuffix);
            await _poisonQueueReader.AddMessageAsync(newMessageVersion);
            await _queueReader.FinishMessageAsync(message);

            if (_shouldNotify)
                await _slackNotifier.ErrorAsync($"Msg put to {_queueName + PoisonSuffix}, data: {newMessageVersion}");
        }

        private Task LogError(string component, string process, Exception ex)
        {
            try
            {
                return _log.WriteErrorAsync(component, process, null, ex);
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"Error in logger: {logEx.Message}. Trace: {logEx.StackTrace}");
                return Task.CompletedTask;
            }
        }
    }
}

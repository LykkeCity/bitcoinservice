using System;
using System.Threading.Tasks;
using BackgroundWorker.Commands;
using BackgroundWorker.Handlers;
using Common;
using Common.Log;
using Core;
using LkeServices.Triggers.Attributes;

namespace BackgroundWorker.Functions
{
    public class QueueCommandFunction
    {
        private readonly HandlersFactory _factory;
        private readonly ILog _logger;

        public QueueCommandFunction(HandlersFactory factory, ILog logger)
        {
            _factory = factory;
            _logger = logger;
        }

        [QueueTrigger(Constants.InDataQueue, 5000)]
        public async Task IncomingCommandExecute(string message)
        {
            try
            {
                var command = message.DeserializeJson<CommandData>();
                var handler = _factory.Create(command.Type);
                await handler.Execute(command.Data);
            }
            catch (Exception ex)
            {
                await _logger.WriteErrorAsync("QueueCommandFunction", "IncomingCommandExecute", null, ex);
                throw;
            }
        }

    }
}

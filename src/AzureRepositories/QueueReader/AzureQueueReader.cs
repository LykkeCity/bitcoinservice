using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Core.QueueReader;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using IQueueReader = Core.QueueReader.IQueueReader;

namespace AzureRepositories.QueueReader
{
    public class AzureMessage : IQueueMessage
    {
        internal readonly CloudQueueMessage Msg;

        public AzureMessage(CloudQueueMessage msg)
        {
            Msg = msg;
        }

        public object Value(Type type)
        {
            if (Msg == null)
                return null;
            if (type == typeof(string))
                return Msg.AsString;
            if (type.GetTypeInfo().IsValueType)
                return Convert.ChangeType(Msg.AsString, type);

            return JsonConvert.DeserializeObject(Msg.AsString, type);
        }

        public string AsString => Msg.AsString;

        public int DequeueCount => Msg.DequeueCount;

        public DateTimeOffset InsertionTime => Msg.InsertionTime ?? DateTimeOffset.UtcNow;
    }


    public class AzureQueueReader : IQueueReader
    {
        private readonly int _visibilityTimeoutSeconds = (int)TimeSpan.FromMinutes(10).TotalSeconds;

        private readonly IQueueExt _queue;

        public AzureQueueReader(IQueueExt queue)
        {
            _queue = queue;
        }

        public async Task<IQueueMessage> GetMessageAsync()
        {
            var msg = await _queue.GetRawMessageAsync(_visibilityTimeoutSeconds);
            if (msg != null)
                return new AzureMessage(msg);
            return null;
        }

        public Task AddMessageAsync(string message)
        {
            return _queue.PutRawMessageAsync(message);
        }

        public Task FinishMessageAsync(IQueueMessage msg)
        {
            var internalMsg = (msg as AzureMessage)?.Msg;
            if (internalMsg != null)
                return _queue.FinishRawMessageAsync(internalMsg);
            return Task.CompletedTask;
        }

        public Task ReleaseMessageAsync(IQueueMessage msg)
        {
            var internalMsg = (msg as AzureMessage)?.Msg;
            if (internalMsg != null)
                return _queue.ReleaseRawMessageAsync(internalMsg);
            return Task.CompletedTask;
        }

        public async Task<int> Count()
        {
            return await _queue.Count() ?? 0;
        }
    }
}

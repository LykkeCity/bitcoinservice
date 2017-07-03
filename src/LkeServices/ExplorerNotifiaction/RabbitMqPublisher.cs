using System;
using System.Collections.Generic;
using System.Text;
using Core;
using Core.ExplorerNotification;
using Core.Settings;
using RabbitMQ.Client;

namespace LkeServices.ExplorerNotifiaction
{
    public class RabbitMqPublisher : IRabbitMqPublisher
    {
        private readonly RabbitMqSettings _settings;
        private readonly string _queue;
        private readonly IModel _channel;

        public RabbitMqPublisher(RabbitMqSettings settings, string queue)
        {
            _settings = settings;
            _queue = queue;
            if (string.IsNullOrEmpty(settings.ExternalHost)) return;            
            var factory = new ConnectionFactory
            {
                HostName = settings.ExternalHost,
                Port = settings.Port,
                UserName = settings.Username,
                Password = settings.Password,
                AutomaticRecoveryEnabled = true                
            };
            var connection = factory.CreateConnection();
            _channel = connection.CreateModel();

            _channel.QueueDeclare(queue, durable: false, exclusive: false, autoDelete: false);
        }

        public void Publish(string data)
        {           
            _channel?.BasicPublish(_settings.Exchange, _queue, body: Encoding.UTF8.GetBytes(data));
        }
    }
}

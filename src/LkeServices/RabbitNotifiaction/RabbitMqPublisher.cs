using System;
using System.Text;
using Core.RabbitNotification;
using RabbitMQ.Client;

namespace LkeServices.RabbitNotifiaction
{
    public class RabbitMqPublisher : IRabbitMqPublisher
    {
        private readonly string _exchange;        
        private readonly IModel _channel;

        public RabbitMqPublisher(string connectionString, string exchange)
        {
            _exchange = exchange;            
            if (string.IsNullOrEmpty(connectionString)) return;

            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString),
                AutomaticRecoveryEnabled = true
            };

            var connection = factory.CreateConnection();
            _channel = connection.CreateModel();

            _channel.ExchangeDeclare(exchange, "fanout", true);
        }

        public void Publish(string data)
        {
            _channel?.BasicPublish(_exchange, string.Empty, body: Encoding.UTF8.GetBytes(data));
        }
    }
}

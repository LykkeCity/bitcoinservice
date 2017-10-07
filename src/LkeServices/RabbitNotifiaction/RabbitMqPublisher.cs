using System;
using System.Text;
using Common.Log;
using Core.RabbitNotification;
using RabbitMQ.Client;

namespace LkeServices.RabbitNotifiaction
{
    public class RabbitMqPublisher : IRabbitMqPublisher
    {
        private readonly string _connectionString;
        private readonly string _exchange;
        private readonly ILog _logger;
        private IModel _channel;

        public RabbitMqPublisher(string connectionString, string exchange, ILog logger)
        {
            _connectionString = connectionString;
            _exchange = exchange;
            _logger = logger;
            EnsureConnection();
        }

        private void EnsureConnection()
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            if (_channel != null) return;

            var factory = new ConnectionFactory
            {
                Uri = new Uri(_connectionString),
                AutomaticRecoveryEnabled = true
            };
            try
            {
                var connection = factory.CreateConnection();
                _channel = connection.CreateModel();
                _channel.ExchangeDeclare(_exchange, "fanout", true);
            }
            catch (Exception ex)
            {
                _logger.WriteErrorAsync(nameof(RabbitMqPublisher), nameof(EnsureConnection), null, ex);
            }
        }

        public void Publish(string data)
        {
            try
            {
                EnsureConnection();
                _channel?.BasicPublish(_exchange, string.Empty, body: Encoding.UTF8.GetBytes(data));
            }
            catch (Exception ex)
            {
                _logger.WriteErrorAsync(nameof(RabbitMqPublisher), nameof(Publish), data, ex);
            }
        }
    }
}

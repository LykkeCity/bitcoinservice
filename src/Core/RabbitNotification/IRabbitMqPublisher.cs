namespace Core.RabbitNotification
{
    public interface IRabbitMqPublisher
    {
        void Publish(string data);
    }
}

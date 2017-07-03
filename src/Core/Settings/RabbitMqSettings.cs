namespace Core.Settings
{
    public class RabbitMqSettings
    {        
        public string ExternalHost { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }        
        public string Exchange { get; set; }
    }
}

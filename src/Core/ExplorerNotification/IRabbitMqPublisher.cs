using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.ExplorerNotification
{
    public interface IRabbitMqPublisher
    {
        void Publish(string data);
    }
}

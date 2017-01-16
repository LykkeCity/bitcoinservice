using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.QueueReader
{
    public interface IQueueReaderFactory
    {
	    IQueueReader Create(string queueName);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Core.QueueReader;
using IQueueReader = Core.QueueReader.IQueueReader;

namespace AzureRepositories.QueueReader
{
    public class AzureQueueReaderFactory : IQueueReaderFactory
    {
	    private readonly string _connectionString;

	    public AzureQueueReaderFactory(string connectionString)
	    {
		    _connectionString = connectionString;
	    }

	    public IQueueReader Create(string queueName)
	    {		   
		    return new AzureQueueReader(new AzureQueueExt(_connectionString, queueName));
	    }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoRepositories.Mongo
{
    public interface IMongoBlobStorage
    {
        Task<string> SaveBlobAsync(string container, string key, Stream stream);

        Task<Stream> GetAsync(string container, string key);
	    Task<bool> HasBlobAsync(string containerName, string blobKey);
	    Task<DateTime?> GetBlobsLastModifiedAsync(string containerName);
		Task<string> SaveBlobAsync(string containerName, string fileName, byte[] v);
	    Task<string> GetAsTextAsync(string containerName, string fileName);
    }
}

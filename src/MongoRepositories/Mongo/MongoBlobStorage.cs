using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace MongoRepositories.Mongo
{
	public class MongoBlobStorage : IMongoBlobStorage
	{
		private readonly IMongoDatabase _db;

		public MongoBlobStorage(IMongoClient mongoClient, string dbName = "storagedb")
		{
			_db = mongoClient.GetDatabase(dbName);
		}

		private GridFSBucket GetBucket(string name)
		{
			return new GridFSBucket(_db, new GridFSBucketOptions
			{
				BucketName = name,
				ChunkSizeBytes = 1048576 // 1MB
			});
		}

		public async Task<string> SaveBlobAsync(string container, string key, Stream stream)
		{
			var bucket = GetBucket(container);
			var id = await bucket.UploadFromStreamAsync(key, stream);
			return id.ToString();
		}

		public async Task<Stream> GetAsync(string container, string key)
		{
			var bucket = GetBucket(container);
			var stream = new MemoryStream();

			await bucket.DownloadToStreamByNameAsync(key, stream);

			stream.Seek(0, SeekOrigin.Begin);

			return stream;
		}

		public Task<bool> HasBlobAsync(string containerName, string blobKey)
		{
			var bucket = GetBucket(containerName);
			return bucket.Find(new FilterDefinitionBuilder<GridFSFileInfo>().Eq(o => o.Filename, blobKey)).AnyAsync();
		}

		public async Task<DateTime?> GetBlobsLastModifiedAsync(string containerName)
		{
			var bucket = GetBucket(containerName);

			var sort = Builders<GridFSFileInfo>.Sort.Descending(x => x.UploadDateTime);
			var options = new GridFSFindOptions
			{
				Limit = 1,
				Sort = sort
			};
			using (var cursor = await bucket.FindAsync(FilterDefinition<GridFSFileInfo>.Empty, options))
			{
				var fileInfo = cursor.ToList().FirstOrDefault();
				return fileInfo?.UploadDateTime;
			}
		}

		public async Task<string> SaveBlobAsync(string containerName, string fileName, byte[] bytes)
		{
			var bucket = GetBucket(containerName);
			var id = await bucket.UploadFromBytesAsync(fileName, bytes);
			return id.ToString();
		}

		public async Task<string> GetAsTextAsync(string containerName, string fileName)
		{
			var bucket = GetBucket(containerName);
			return Encoding.UTF8.GetString(await bucket.DownloadAsBytesByNameAsync(fileName));
		}
	}
}

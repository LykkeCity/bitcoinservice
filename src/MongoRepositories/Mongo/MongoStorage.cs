using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoRepositories.Utils;
namespace MongoRepositories.Mongo
{
	public class MongoStorage<T> : IMongoStorage<T> where T : MongoEntity
	{
		private readonly IMongoCollection<T> _collection;

		public MongoStorage(IMongoClient mongoClient, string tableName, string dbName = "maindb")
		{
			var db = mongoClient.GetDatabase(dbName);
			_collection = db.GetCollection<T>(tableName);
		}

		public async Task<IEnumerable<T>> GetDataAsync()
		{
			return await RetryQuery(async () => await _collection.Find(FilterDefinition<T>.Empty).ToListAsync());
		}

		public async Task<T> GetDataAsync(string key)
		{
			return await RetryQuery(async () => await _collection.Find(x => x.BsonId == key).FirstOrDefaultAsync());
		}

		public async Task<IEnumerable<T>> GetDataAsync(Expression<Func<T, bool>> filter)
		{
			return await RetryQuery(async () => await _collection.Find(filter).ToListAsync());
		}

		public async Task<T> InsertOrReplaceAsync(T item)
		{
			while (true)
			{
				try
				{
					var entity = await GetDataAsync(item.BsonId);

					if (entity == null)
					{
						await _collection.InsertOneAsync(item);
						return item;
					}

					item.BsonVersion = entity.BsonVersion + 1;
					item.BsonUpdateDt = DateTime.UtcNow;
					var output = await _collection.ReplaceOneAsync(x => x.BsonId == item.BsonId && x.BsonVersion == entity.BsonVersion, item);
					if (output.ModifiedCount > 0)
						return item;
				}
				catch (MongoWriteException ex)
				{
					if (!ex.IsDuplicateError())
						throw;
				}
			}
		}

		public Task DeleteAsync(Expression<Func<T, bool>> filter)
		{
			return RetryQuery(() => _collection.DeleteManyAsync(filter));
		}

		public async Task<T> InsertOrModifyAsync(string key, Func<T> createNew, Func<T, T> modify)
		{
			while (true)
			{
				try
				{
					var entity = await GetDataAsync(key);

					T item;

					if (entity == null)
					{
						item = createNew();
						await _collection.InsertOneAsync(item);
						return item;
					}
					var prevVersion = entity.BsonVersion;
					item = modify(entity);
					item.BsonVersion = prevVersion + 1;
					item.BsonUpdateDt = DateTime.UtcNow;
					var output = await _collection.ReplaceOneAsync(x => x.BsonId == item.BsonId && x.BsonVersion == prevVersion, item);
					if (output.ModifiedCount > 0)
						return item;
				}
				catch (MongoWriteException ex)
				{
					if (!ex.IsDuplicateError())
						throw;
				}
			}
		}

		public Task GetDataByChunksAsync(Action<IEnumerable<T>> action)
		{
			return ExecuteQueryAsync(null, itms =>
			{
				action(itms);
				return true;
			});
		}

		public Task InsertAsync(T item)
		{
			return _collection.InsertOneAsync(item);
		}

		public Task DeleteAsync(T record)
		{
			if (record != null)
				return DeleteAsync(record.BsonId);
			return Task.CompletedTask;
		}

		public async Task InsertOrMergeAsync(T entity)
		{
			while (true)
			{
				try
				{
					var record = await GetDataAsync(entity.BsonId);

					if (record != null)
					{
						var prevVersion = record.BsonVersion;
						var currentDoc = record.ToBsonDocument();
						var merged = currentDoc.MergeExt(entity.ToBsonDocument());
						var replace = BsonSerializer.Deserialize<T>(merged);
						replace.BsonVersion = prevVersion + 1;
						replace.BsonUpdateDt = DateTime.UtcNow;
						var output = await _collection.ReplaceOneAsync(x => x.BsonId == record.BsonId && x.BsonVersion == prevVersion, replace);
						if (output.ModifiedCount > 0)
							return;
					}
					else
					{
						await _collection.InsertOneAsync(entity);
						return;
					}

				}
				catch (MongoWriteException ex)
				{
					if (!ex.IsDuplicateError())
						throw;
				}
			}
		}

		public async Task<T> GetTopRecordAsync(Expression<Func<T, bool>> filter, Expression<Func<T, object>> sortSelector, SortDirection direction)
		{
		    return (await GetTopRecordsAsync(filter, sortSelector, direction, 1)).FirstOrDefault();
		}

	    public async Task<IEnumerable<T>> GetTopRecordsAsync(Expression<Func<T, bool>> filter, Expression<Func<T, object>> sortSelector, SortDirection direction, int limit)
	    {
	        return await RetryQuery(async () =>
	        {               
	            var query = _collection.Find(filter);
	            query = direction == SortDirection.Ascending ? query.SortBy(sortSelector) : query.SortByDescending(sortSelector);
	            return await query.Limit(limit).ToListAsync();
	        });
        }

	    public async Task InsertAsync(IEnumerable<T> documents)
		{
			if (documents.Any())
			{
				var batchId = Guid.NewGuid();

				foreach (var document in documents)
					document.BatchId = batchId;

				try
				{
					await _collection.InsertManyAsync(documents);
				}
				catch (Exception)
				{
					await _collection.DeleteManyAsync(o => o.BatchId == batchId);
					throw;
				}
			}
		}

		public Task ScanDataAsync(Func<IEnumerable<T>, Task> chunk)
		{
			return ExecuteQueryAsync(null, chunk);
		}

		public Task ScanDataAsync(Func<T, bool> filter, Func<IEnumerable<T>, Task> chunk)
		{
			return ExecuteQueryAsync(filter, chunk);
		}

		public Task InsertOrReplaceBatchAsync(T[] entities)
		{
			return _collection.BulkWriteAsync(entities.Select(d =>
				new ReplaceOneModel<T>(new FilterDefinitionBuilder<T>().Eq(x => x.BsonId, d.BsonId), d)
				{
					IsUpsert = true
				}));
		}

		public async Task<T> InsertAndGenerateRowKeyAsDateTimeAsync(T entity, DateTime dt, bool shortFormat = false)
		{
			int n = 0;
			var id = entity.BsonId;
			while (true)
			{
				try
				{
					var suffix = dt + (shortFormat ? n.ToString("000") : '.' + n.ToString("000"));
					entity.BsonId = id
						+ "_" + suffix;
					await InsertAsync(entity);
					return entity;
				}
				catch (MongoWriteException ex)
				{
					if (!ex.IsDuplicateError())
						throw;
				}
				n++;
				if (n == 999)
					throw new Exception("Can't insert item");
			}
		}

		public async Task<T> FirstOrNullViaScanAsync(Func<IEnumerable<T>, T> dataToSearch)
		{
			T result = null;

			await ExecuteQueryAsync(null,
				itms =>
				{
					result = dataToSearch(itms);
					return result == null;
				});

			return result;
		}

		public async Task<T> InsertAndGenerateRowKeyAsTimeAsync(T newEntity, DateTime dateTime)
		{
			int n = 0;
			var dt = dateTime.ToString("HH:mm:ss");
			while (true)
			{
				try
				{
					newEntity.BsonId = dt + '.' + n.ToString("000");
					await InsertAsync(newEntity);
					return newEntity;
				}
				catch (MongoWriteException ex)
				{
					if (!ex.IsDuplicateError())
						throw;
				}
				n++;
				if (n == 999)
					throw new Exception("Can't insert item");
			}
		}


		public async Task<T> MergeAsync(string key, Func<T, T> func)
		{
			while (true)
			{
				var entity = await GetDataAsync(key);
				if (entity == null) return null;

				var prevVersion = entity.BsonVersion;

				var currentDoc = entity.ToBsonDocument();

				var newDoc = func(entity).ToBsonDocument();

				var merged = currentDoc.MergeExt(newDoc);

				var item = BsonSerializer.Deserialize<T>(merged);
				item.BsonVersion = prevVersion + 1;
				item.BsonUpdateDt = DateTime.UtcNow;
				var output = await _collection.ReplaceOneAsync(x => x.BsonId == entity.BsonId && x.BsonVersion == prevVersion, item);
				if (output.ModifiedCount > 0)
					return item;
			}
		}

		public async Task<T> ReplaceAsync(string key, Func<T, T> replaceAction)
		{
			while (true)
			{
				var entity = await GetDataAsync(key);
				if (entity == null)
					return null;

				var prevVersion = entity.BsonVersion;

				var result = replaceAction(entity);
				if (result == null)
					return null;

				result.BsonVersion = prevVersion + 1;
				result.BsonUpdateDt = DateTime.UtcNow;
				var output = await _collection.ReplaceOneAsync(x => x.BsonId == key && x.BsonVersion == prevVersion, result);
				if (output.ModifiedCount > 0)
					return result;
			}
		}

		public Task DeleteAsync(string id)
		{
			return RetryQuery(() => _collection.DeleteOneAsync(o => o.BsonId == id));
		}

		public void DeleteAll()
		{
			_collection.DeleteMany(o => true);
		}


		private async Task ExecuteQueryAsync(Func<T, bool> filter, Func<IEnumerable<T>, bool> yieldData)
		{
			int skip = 0;
			int limit = 10;
			do
			{
				var queryResponse = await RetryQuery(async () => await _collection.Find(FilterDefinition<T>.Empty).Skip(skip).Limit(limit).ToListAsync());
				var shouldWeContinue = yieldData(filter != null ? queryResponse.Where(filter) : queryResponse);
				if (!shouldWeContinue)
					break;
				if (queryResponse.Count == 0)
					return;
				skip += limit;
			}
			while (true);
		}

		private async Task ExecuteQueryAsync(Func<T, bool> filter, Func<IEnumerable<T>, Task> yieldData)
		{
			int skip = 0;
			int limit = 10;
			do
			{
				var queryResponse = await RetryQuery(async () => await _collection.Find(FilterDefinition<T>.Empty).Skip(skip).Limit(limit).ToListAsync());
				await yieldData(filter != null ? queryResponse.Where(filter) : queryResponse);
				if (queryResponse.Count == 0)
					return;
				skip += limit;
			}
			while (true);
		}

		public async Task<bool> Any(Expression<Func<T, bool>> filter)
		{
			return (await RetryQuery(async () => await _collection.FindAsync(filter, new FindOptions<T>() { Limit = 1 }))).Any();
		}

		public static async Task<TIn> RetryQuery<TIn>(Func<Task<TIn>> action)
		{
			const int tryCount = 5;
			var @try = 0;
			int delay = 100;
			while (true)
			{
				try
				{
					return await action();
				}
				catch (Exception ex)
				{
					@try++;                    
					if (!(ex is MongoException) || @try >= tryCount)
						throw;
					await Task.Delay(delay);
					delay *= 3;
				}
			}
		}

	}
}

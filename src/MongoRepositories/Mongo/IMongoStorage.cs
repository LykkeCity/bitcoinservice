using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoRepositories.Mongo
{
    public interface IMongoStorage<T> where T : MongoEntity
    {
        Task<IEnumerable<T>> GetDataAsync();
        Task<T> GetDataAsync(string key);
	    Task<IEnumerable<T>> GetDataAsync(Expression<Func<T, bool>> filter);

		Task<T> InsertOrReplaceAsync(T item);
        Task<T> MergeAsync(string key, Func<T, T> func);
        Task<T> ReplaceAsync(string key, Func<T, T> func);
	    Task DeleteAsync(string id);
	    Task DeleteAsync(Expression<Func<T, bool>> filter);
	    Task<T> InsertOrModifyAsync(string key, Func<T> createNew, Func<T, T> modify);

	    Task GetDataByChunksAsync(Action<IEnumerable<T>> action);
	    Task InsertAsync(T item);
		Task DeleteAsync(T record);
	    Task InsertOrMergeAsync(T entity);

	    Task<T> GetTopRecordAsync(Expression<Func<T, bool>> filter, Expression<Func<T, object>> sortSelector,
		    SortDirection direction);

	    Task InsertAsync(IEnumerable<T> documents);
	    Task ScanDataAsync(Func<IEnumerable<T>, Task> chunk);
        Task ScanDataAsync(Func<T, bool> filter, Func<IEnumerable<T>, Task> chunk);
        Task InsertOrReplaceBatchAsync(T[] entities);
	    Task<T> InsertAndGenerateRowKeyAsDateTimeAsync(T entity, DateTime dt, bool shortFormat = false);

	    Task<T> FirstOrNullViaScanAsync(Func<IEnumerable<T>, T> dataToSearch);

	    Task<T> InsertAndGenerateRowKeyAsTimeAsync(T newEntity, DateTime dt);
        Task<bool> Any(Expression<Func<T, bool>> filter);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using Common.Log;
using Core.Repositories.ApiRequests;

namespace AzureRepositories.ApiRequests
{
    public class ApiRequestBlobRepository : IApiRequestBlobRepository
    {
        private const string BlobContainer = "api-requests";

        private readonly IBlobStorage _blobStorage;

        public ApiRequestBlobRepository(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage;
        }

        public async  Task LogToBlob(Guid id, string type, string data)
        {
            try
            {
                await _blobStorage.SaveBlobAsync(BlobContainer, $"{id}.{type}.txt", Encoding.UTF8.GetBytes(data));
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}

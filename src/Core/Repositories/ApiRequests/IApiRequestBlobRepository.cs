using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.ApiRequests
{
    public interface IApiRequestBlobRepository
    {
        Task LogToBlob(Guid id, string type, string data);
    }
}

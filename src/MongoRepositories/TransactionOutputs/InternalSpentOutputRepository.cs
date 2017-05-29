using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Core.Repositories.TransactionOutputs;
using MongoRepositories.Mongo;

namespace MongoRepositories.TransactionOutputs
{
    public class InternalSpentOutput : MongoEntity, IInternalSpentOutput
    {
        public string TransactionHash { get; set; }
        public int N { get; set; }
    }


    public class InternalSpentOutputRepository : IInternalSpentOutputRepository

    {
        private readonly IMongoStorage<InternalSpentOutput> _table;

        public InternalSpentOutputRepository(IMongoStorage<InternalSpentOutput> table)
        {
            _table = table;
        }

        public async Task<IEnumerable<IInternalSpentOutput>> GetInternalSpentOutputs()
        {
            return await _table.GetDataAsync();
        }
    }
}

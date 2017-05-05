using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Repositories.TransactionOutputs;
using MongoRepositories.Mongo;

namespace MongoRepositories.TransactionOutputs
{
    public class BroadcastedOutputEntity : MongoEntity, IBroadcastedOutput
    {
        public string TransactionHash { get; set; }
        public int N { get; set; }
        public Guid TransactionId { get; set; }
        public string Address { get; set; }
        public string ScriptPubKey { get; set; }
        public string AssetId { get; set; }
        public long Amount { get; set; }
        public long Quantity { get; set; }


        public static string GenerateId(Guid transactionId, int n)
        {
            return transactionId + "_" + n;
        }

        public static BroadcastedOutputEntity Create(IBroadcastedOutput output)
        {
            return new BroadcastedOutputEntity
            {
                BsonId = GenerateId(output.TransactionId, output.N),
                TransactionId = output.TransactionId,
                N = output.N,
                Address = output.Address,
                Amount = output.Amount,
                Quantity = output.Quantity,
                AssetId = output.AssetId,
                ScriptPubKey = output.ScriptPubKey
            };
        }
    }


    public class BroadcastedOutputRepository : IBroadcastedOutputRepository
    {
        private readonly IMongoStorage<BroadcastedOutputEntity> _table;

        public BroadcastedOutputRepository(IMongoStorage<BroadcastedOutputEntity> table)
        {
            _table = table;
        }

        public async Task InsertOutputs(IEnumerable<IBroadcastedOutput> outputs)
        {
            await _table.InsertAsync(outputs.Select(BroadcastedOutputEntity.Create).ToList());
        }

        public async Task<IEnumerable<IBroadcastedOutput>> GetOutputs(string address)
        {
            return await _table.GetDataAsync(entity => entity.Address == address && !string.IsNullOrEmpty(entity.TransactionHash));
        }

        public async Task SetTransactionHash(Guid transactionId, string transactionHash)
        {            
            var records = await _table.GetDataAsync(o=>o.TransactionId == transactionId);

            var tasks = new List<Task>();
            foreach (var rec in records)
            {
                tasks.Add(_table.ReplaceAsync(rec.BsonId, entity =>
                {
                    entity.TransactionHash = transactionHash;
                    return entity;
                }));         
            }
            await Task.WhenAll(tasks);
        }

        public async Task DeleteOutput(string transactionHash, int n)
        {
            await _table.DeleteAsync(o=>o.TransactionHash == transactionHash && o.N == n);           
        }       
    }
}

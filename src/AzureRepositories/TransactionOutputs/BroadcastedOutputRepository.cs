using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.TransactionOutputs;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.TransactionOutputs
{
    public class BroadcastedOutputEntity : TableEntity, IBroadcastedOutput
    {
        public string TransactionHash { get; set; }
        public int N { get; set; }
        public Guid TransactionId { get; set; }
        public string Address { get; set; }
        public string ScriptPubKey { get; set; }
        public string AssetId { get; set; }
        public long Amount { get; set; }
        public long Quantity { get; set; }

        public static class ByTransactionId
        {
            public static string GeneratePartition(Guid transactionId)
            {
                return transactionId.ToString();
            }

            public static BroadcastedOutputEntity Create(IBroadcastedOutput output)
            {
                return new BroadcastedOutputEntity
                {
                    RowKey = output.N.ToString(),
                    PartitionKey = GeneratePartition(output.TransactionId),
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

        public static class ByAddress
        {
            public static string GeneratePartition(string address)
            {
                return address;
            }

            public static string GenerateRowKey(Guid transactionId, int n)
            {
                return $"{transactionId}_{n}";
            }

            public static BroadcastedOutputEntity Create(IBroadcastedOutput output)
            {
                return new BroadcastedOutputEntity
                {
                    RowKey = GenerateRowKey(output.TransactionId, output.N),
                    PartitionKey = GeneratePartition(output.Address),
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

        public static class ByTransactionHash
        {
            public static string GeneratePartition(string transactionHash)
            {
                return transactionHash;
            }

            public static BroadcastedOutputEntity Create(IBroadcastedOutput output)
            {
                return new BroadcastedOutputEntity
                {
                    RowKey = output.N.ToString(),
                    PartitionKey = GeneratePartition(output.TransactionHash),
                    TransactionId = output.TransactionId,
                    TransactionHash = output.TransactionHash,
                    N = output.N,
                    Address = output.Address,
                    Amount = output.Amount,
                    Quantity = output.Quantity,
                    AssetId = output.AssetId,
                    ScriptPubKey = output.ScriptPubKey
                };
            }
        }
    }


    public class BroadcastedOutputRepository : IBroadcastedOutputRepository
    {
        private readonly INoSQLTableStorage<BroadcastedOutputEntity> _table;

        public BroadcastedOutputRepository(INoSQLTableStorage<BroadcastedOutputEntity> table)
        {
            _table = table;
        }

        public async Task InsertOutputs(IEnumerable<IBroadcastedOutput> outputs)
        {
            await _table.InsertAsync(outputs.Select(BroadcastedOutputEntity.ByTransactionId.Create));

            foreach (var addressGroup in outputs.GroupBy(o => o.Address))
            {
                await _table.InsertAsync(addressGroup.Select(BroadcastedOutputEntity.ByAddress.Create));
            }
        }

        public async Task<IEnumerable<IBroadcastedOutput>> GetOutputs(string address)
        {
            return await _table.GetDataAsync(BroadcastedOutputEntity.ByAddress.GeneratePartition(address),
                entity => !string.IsNullOrEmpty(entity.TransactionHash));
        }

        public async Task SetTransactionHash(Guid transactionId, string transactionHash)
        {
            var records = await _table.GetDataAsync(BroadcastedOutputEntity.ByTransactionId.GeneratePartition(transactionId));

            foreach (var rec in records)
            {
                await _table.ReplaceAsync(BroadcastedOutputEntity.ByTransactionId.GeneratePartition(rec.TransactionId), rec.N.ToString(), entity =>
                {
                    entity.TransactionHash = transactionHash;
                    return entity;
                });
                await _table.ReplaceAsync(BroadcastedOutputEntity.ByAddress.GeneratePartition(rec.Address), BroadcastedOutputEntity.ByAddress.GenerateRowKey(rec.TransactionId, rec.N), entity =>
                {
                    entity.TransactionHash = transactionHash;
                    return entity;
                });
                rec.TransactionHash = transactionHash;
                await _table.InsertOrReplaceAsync(BroadcastedOutputEntity.ByTransactionHash.Create(rec));
            }
        }

        public async Task DeleteOutput(string transactionHash, int n)
        {
            var output = await _table.DeleteAsync(BroadcastedOutputEntity.ByTransactionHash.GeneratePartition(transactionHash), n.ToString());
            if (output != null)
            {
                await _table.DeleteAsync(BroadcastedOutputEntity.ByTransactionId.GeneratePartition(output.TransactionId), output.N.ToString());
                await _table.DeleteAsync(BroadcastedOutputEntity.ByAddress.GeneratePartition(output.Address), BroadcastedOutputEntity.ByAddress.GenerateRowKey(output.TransactionId, output.N));
            }
        }

        public Task<bool> OutputExists(string transactionHash, int n)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IBroadcastedOutput>> GetOldOutputs(DateTime bound, int limit)
        {
            throw new NotImplementedException();
        }

        public Task DeleteBroadcastedOutputs(IEnumerable<IBroadcastedOutput> outputs)
        {
            throw new NotImplementedException();
        }
    }
}

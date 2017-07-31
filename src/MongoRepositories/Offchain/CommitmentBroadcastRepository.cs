using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Repositories.Offchain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoRepositories.Mongo;

namespace MongoRepositories.Offchain
{
    public class CommitmentBroadcastEntity : MongoEntity, ICommitmentBroadcast
    {
        [BsonRepresentation(BsonType.String)]
        public Guid CommitmentId { get; set; }
        public string TransactionHash { get; set; }
        public DateTime Date { get; set; }
        public CommitmentBroadcastType Type { get; set; }
        public decimal ClientAmount { get; set; }
        public decimal HubAmount { get; set; }
        public decimal RealClientAmount { get; set; }
        public decimal RealHubAmount { get; set; }
        public string PenaltyTransactionHash { get; set; }

        public static CommitmentBroadcastEntity Create(Guid commitmentId, string transactionHash, CommitmentBroadcastType type, decimal clientAmount,
            decimal hubAmount,
            decimal realClientAmount, decimal realHubAmount, string penaltyHash)
        {
            return new CommitmentBroadcastEntity
            {
                BsonId = Guid.NewGuid().ToString(),
                CommitmentId = commitmentId,
                TransactionHash = transactionHash,
                Type = type,
                ClientAmount = clientAmount,
                HubAmount = hubAmount,
                RealClientAmount = realClientAmount,
                RealHubAmount = realHubAmount,
                Date = DateTime.UtcNow,
                PenaltyTransactionHash = penaltyHash
            };
        }
    }



    public class CommitmentBroadcastRepository : ICommitmentBroadcastRepository
    {
        private readonly IMongoStorage<CommitmentBroadcastEntity> _table;

        public CommitmentBroadcastRepository(IMongoStorage<CommitmentBroadcastEntity> table)
        {
            _table = table;
        }

        public async Task<ICommitmentBroadcast> InsertCommitmentBroadcast(Guid commitmentId, string transactionHash, CommitmentBroadcastType type, decimal clientAmount, decimal hubAmount, decimal realClientAmount, decimal realHubAmount, string penaltyHash)
        {
            var entity = CommitmentBroadcastEntity.Create(commitmentId, transactionHash, type, clientAmount, hubAmount, realClientAmount,
                realHubAmount, penaltyHash);
            await _table.InsertAsync(entity);
            return entity;
        }

        public async Task SetPenaltyTransactionHash(Guid commitmentId, string hash)
        {
            var entity = (await _table.GetDataAsync(o => o.CommitmentId == commitmentId)).FirstOrDefault();
            if (entity != null)
            {
                await _table.ReplaceAsync(entity.BsonId, broadcastEntity =>
                 {
                     broadcastEntity.PenaltyTransactionHash = hash;
                     return broadcastEntity;
                 });
            }
        }

        public async Task<IEnumerable<ICommitmentBroadcast>> GetLastCommitmentBroadcasts(int limit)
        {
            return await _table.GetTopRecordsAsync(o => true, o => o.Date, SortDirection.Descending, limit);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Helpers;
using Core.Repositories.Offchain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoRepositories.Mongo;

namespace MongoRepositories.Offchain
{
    public class CommitmentEntity : MongoEntity, ICommitment
    {
        [BsonIgnore]
        public Guid CommitmentId => Guid.Parse(BsonId);

        public CommitmentType Type { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Guid ChannelId { get; set; }

        public string InitialTransaction { get; set; }
        public string Multisig { get; set; }
        public string AssetId { get; set; }
        public string SignedTransaction { get; set; }
        public string RevokePubKey { get; set; }

        public decimal ClientAmount { get; set; }
        public decimal HubAmount { get; set; }

        public string LockedAddress { get; set; }
        public string LockedScript { get; set; }

        public DateTime CreateDt { get; set; }

        public bool Actual { get; set; }


        public static string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }

        public static CommitmentEntity Create(Guid channelTransactionId, CommitmentType type, string multisig,
            string asset,
            string revokePubKey, string initialTr, decimal clientAmount, decimal hubAmount, string lockedAddress, string lockedScript)
        {
            return new CommitmentEntity
            {
                BsonId = GenerateId(),
                ChannelId = channelTransactionId,
                Multisig = multisig,
                AssetId = asset,
                Type = type,
                RevokePubKey = revokePubKey,
                InitialTransaction = initialTr,
                ClientAmount = clientAmount,
                HubAmount = hubAmount,
                LockedAddress = lockedAddress,
                LockedScript = lockedScript,
                CreateDt = DateTime.UtcNow,
                Actual = true
            };
        }

    }



    public class CommitmentRepository : ICommitmentRepository
    {
        private readonly IMongoStorage<CommitmentEntity> _table;

        public CommitmentRepository(IMongoStorage<CommitmentEntity> table)
        {
            _table = table;
        }

        public async Task<ICommitment> CreateCommitment(CommitmentType type, Guid channelTransactionId, string multisig, string asset,
            string revokePubKey, string initialTr, decimal clientAmount, decimal hubAmount, string lockedAddress, string lockedScript)
        {
            var entity = CommitmentEntity.Create(channelTransactionId, type, multisig, asset, revokePubKey,
                initialTr, clientAmount, hubAmount, lockedAddress, lockedScript);
            await _table.InsertAsync(entity);
            return entity;
        }

        public async Task<ICommitment> GetLastCommitment(string multisig, string asset, CommitmentType type)
        {
            return await _table.GetTopRecordAsync(o => o.Multisig == multisig && o.AssetId == asset && o.Type == type &&
                o.Actual,
                o => o.CreateDt, SortDirection.Descending);
        }

        public async Task SetFullSignedTransaction(Guid commitmentId, string multisig, string asset, string fullSignedCommitment)
        {
            await _table.ReplaceAsync(commitmentId.ToString(), entity =>
            {
                entity.SignedTransaction = fullSignedCommitment;
                return entity;
            });
        }

        public async Task<IEnumerable<ICommitment>> GetMonitoringCommitments()
        {
            return await _table.GetDataAsync(o => o.Actual);
        }

        public async Task CloseCommitmentsOfChannel(string multisig, string asset, Guid channelId)
        {
            var commitments = await _table.GetDataAsync(o => o.ChannelId == channelId && o.Actual);

            var tasks = new List<Task>();

            foreach (var commitment in commitments)
            {
                tasks.Add(_table.ReplaceAsync(commitment.CommitmentId.ToString(), entity =>
               {
                   entity.Actual = false;
                   return entity;
               }));
            }

            await Task.WhenAll(tasks);
        }

        public async Task<ICommitment> GetCommitment(string multisig, string asset, string transactionHex)
        {
            return (await _table.GetDataAsync(o => o.Multisig == multisig && o.AssetId == asset && o.Actual)).FirstOrDefault(o => TransactionComparer.CompareTransactions(o.InitialTransaction, transactionHex));
        }

        public async Task RemoveCommitmentsOfChannel(string multisig, string asset, Guid channelId)
        {
            await _table.DeleteAsync(o => o.Multisig == multisig && o.AssetId == asset && o.ChannelId == channelId && o.Actual);
        }
    }
}

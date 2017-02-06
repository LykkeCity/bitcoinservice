using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.Offchain;

namespace AzureRepositories.Offchain
{
    public class CommitmentEntity : BaseEntity, ICommitment
    {
        public int CommitType { get; set; }

        public Guid CommitmentId => Guid.Parse(RowKey);
        public CommitmentType Type => (CommitmentType)CommitType;

        public Guid ChannelId { get; set; }

        public string InitialTransaction { get; set; }
        public string Multisig { get; set; }
        public string AssetId { get; set; }
        public string SignedTransaction { get; set; }
        public string RevokePrivateKey { get; set; }
        public string RevokePubKey { get; set; }

        public decimal AddedAmount { get; set; }

        public static string GeneratePartition(string multisig, string asset)
        {
            return $"{multisig}_{asset}";
        }

        public static string GenerateRowKey()
        {
            return Guid.NewGuid().ToString();
        }

        public static CommitmentEntity Create(Guid channelTransactionId, CommitmentType type, string multisig, string asset, string revokePrivateKey, string revokePubKey, string initialTr, decimal addedAmount)
        {
            return new CommitmentEntity
            {
                PartitionKey = GeneratePartition(multisig, asset),
                RowKey = GenerateRowKey(),
                ChannelId = channelTransactionId,
                Multisig = multisig,
                AssetId = asset,
                CommitType = (int)type,
                RevokePrivateKey = revokePrivateKey,
                RevokePubKey = revokePubKey,
                InitialTransaction = initialTr,
                AddedAmount = addedAmount
            };
        }
    }



    public class CommitmentRepository : ICommitmentRepository
    {
        private readonly INoSQLTableStorage<CommitmentEntity> _table;

        public CommitmentRepository(INoSQLTableStorage<CommitmentEntity> table)
        {
            _table = table;
        }

        public async Task<ICommitment> CreateCommitment(CommitmentType type, Guid channelTransactionId, string multisig, string asset, string revokePrivateKey, string revokePubKey, string initialTr, decimal addedAmount)
        {
            var entity = CommitmentEntity.Create(channelTransactionId, type, multisig, asset, revokePrivateKey, revokePubKey, initialTr, addedAmount);
            await _table.InsertAsync(entity);
            return entity;
        }

        public async Task<ICommitment> GetLastCommitment(string multisig, string asset, CommitmentType type)
        {
            var partition = CommitmentEntity.GeneratePartition(multisig, asset);
            var commitments = await _table.GetDataAsync(partition, o => o.Type == type);
            return commitments?.OrderByDescending(o => o.Timestamp).FirstOrDefault();
        }

        public async Task SetFullSignedTransaction(Guid commitmentId, string multisig, string asset, string fullSignedCommitment)
        {
            var partition = CommitmentEntity.GeneratePartition(multisig, asset);

            await _table.ReplaceAsync(partition, commitmentId.ToString(), entity =>
            {
                entity.SignedTransaction = fullSignedCommitment;
                return entity;
            });
        }

        public async Task UpdateClientPrivateKey(Guid commitmentId, string multisig, string asset, string privateKey)
        {
            var partition = CommitmentEntity.GeneratePartition(multisig, asset);

            await _table.ReplaceAsync(partition, commitmentId.ToString(), entity =>
            {
                entity.RevokePrivateKey = privateKey;
                return entity;
            });
        }
    }
}

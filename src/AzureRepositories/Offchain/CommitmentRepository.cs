using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.Offchain;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Offchain
{
    public class CommitmentEntity : TableEntity, ICommitment
    {
        public int CommitType { get; set; }

        public Guid CommitmentId => Guid.Parse(RowKey);
        public CommitmentType Type => (CommitmentType)CommitType;

        public string InitialTransaction { get; set; }
        public string Multisig { get; set; }
        public string AssetName { get; set; }
        public string SignedTransaction { get; set; }
        public string RevokePrivateKey { get; set; }
        public string RevokePubKey { get; set; }

        public static string GeneratePartition(string multisig, string assetName)
        {
            return $"{multisig}_{assetName}";
        }

        public static string GenerateRowKey()
        {
            return Guid.NewGuid().ToString();
        }

        public static CommitmentEntity Create(CommitmentType type, string multisig, string assetName,
            string revokePrivateKey,
            string revokePubKey, string initialTr)
        {
            return new CommitmentEntity
            {
                PartitionKey = GeneratePartition(multisig, assetName),
                RowKey = GenerateRowKey(),
                Multisig = multisig,
                AssetName = assetName,
                CommitType = (int)type,
                RevokePrivateKey = revokePrivateKey,
                RevokePubKey = revokePubKey,
                InitialTransaction = initialTr
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

        public async Task<ICommitment> CreateCommitment(CommitmentType type, string multisig, string assetName, string revokePrivateKey,
            string revokePubKey, string initialTr)
        {
            var entity = CommitmentEntity.Create(type, multisig, assetName, revokePrivateKey, revokePubKey, initialTr);
            await _table.InsertAsync(entity);
            return entity;
        }

        public async Task<ICommitment> GetLastCommitment(string multisig, string assetName, CommitmentType type)
        {
            var partition = CommitmentEntity.GeneratePartition(multisig, assetName);
            var commitments = await _table.GetDataAsync(partition, o => o.Type == type);
            return commitments?.OrderByDescending(o => o.Timestamp).FirstOrDefault();
        }

        public async Task SetFullSignedTransaction(Guid commitmentId, string multisig, string assetName, string fullSignedCommitment)
        {
            var partition = CommitmentEntity.GeneratePartition(multisig, assetName);
          
            await _table.ReplaceAsync(partition, commitmentId.ToString(), entity =>
            {
                entity.SignedTransaction = fullSignedCommitment;
                return entity;
            });
        }
    }
}

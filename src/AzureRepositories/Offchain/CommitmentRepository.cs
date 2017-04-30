using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Helpers;
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
        public string RevokePubKey { get; set; }

        public decimal ClientAmount { get; set; }
        public decimal HubAmount { get; set; }

        public string LockedAddress { get; set; }
        public string LockedScript { get; set; }

        public DateTime CreateDt { get; set; }

        public static class ByRecord
        {

            public static string GeneratePartition(string multisig, string asset)
            {
                return $"{multisig}_{asset}";
            }

            public static string GenerateRowKey()
            {
                return Guid.NewGuid().ToString();
            }

            public static CommitmentEntity Create(Guid channelTransactionId, CommitmentType type, string multisig,
                string asset,
                string revokePubKey, string initialTr, decimal clientAmount, decimal hubAmount, string lockedAddress, string lockedScript)
            {
                return new CommitmentEntity
                {
                    PartitionKey = GeneratePartition(multisig, asset),
                    RowKey = GenerateRowKey(),
                    ChannelId = channelTransactionId,
                    Multisig = multisig,
                    AssetId = asset,
                    CommitType = (int)type,
                    RevokePubKey = revokePubKey,
                    InitialTransaction = initialTr,
                    ClientAmount = clientAmount,
                    HubAmount = hubAmount,
                    LockedAddress = lockedAddress,
                    LockedScript = lockedScript,
                    CreateDt = DateTime.UtcNow,
                };
            }
        }

        public static class ByMonitoring
        {

            public static string GeneratePartitionKey()
            {
                return "Monitoring";
            }

            public static CommitmentEntity Create(ICommitment commitment)
            {
                return new CommitmentEntity
                {
                    PartitionKey = GeneratePartitionKey(),
                    RowKey = commitment.CommitmentId.ToString(),
                    ChannelId = commitment.ChannelId,
                    Multisig = commitment.Multisig,
                    AssetId = commitment.AssetId,
                    CommitType = (int)commitment.Type,
                    RevokePubKey = commitment.RevokePubKey,
                    InitialTransaction = commitment.InitialTransaction,
                    ClientAmount = commitment.ClientAmount,
                    HubAmount = commitment.HubAmount,
                    LockedAddress = commitment.LockedAddress,
                    LockedScript = commitment.LockedScript,
                    CreateDt = commitment.CreateDt
                };
            }
        }

        public static class Archive
        {

            public static string GeneratePartitionKey()
            {
                return "Archive";
            }

            public static CommitmentEntity Create(ICommitment commitment)
            {
                return new CommitmentEntity
                {
                    PartitionKey = GeneratePartitionKey(),
                    RowKey = commitment.CommitmentId.ToString(),
                    ChannelId = commitment.ChannelId,
                    Multisig = commitment.Multisig,
                    AssetId = commitment.AssetId,
                    CommitType = (int)commitment.Type,
                    RevokePubKey = commitment.RevokePubKey,
                    InitialTransaction = commitment.InitialTransaction,
                    ClientAmount = commitment.ClientAmount,
                    HubAmount = commitment.HubAmount,
                    LockedAddress = commitment.LockedAddress,
                    LockedScript = commitment.LockedScript,
                    CreateDt = commitment.CreateDt
                };
            }
        }
    }



    public class CommitmentRepository : ICommitmentRepository
    {
        private readonly INoSQLTableStorage<CommitmentEntity> _table;

        public CommitmentRepository(INoSQLTableStorage<CommitmentEntity> table)
        {
            _table = table;
        }

        public async Task<ICommitment> CreateCommitment(CommitmentType type, Guid channelTransactionId, string multisig, string asset,
            string revokePubKey, string initialTr, decimal clientAmount, decimal hubAmount, string lockedAddress, string lockedScript)
        {
            var entity = CommitmentEntity.ByRecord.Create(channelTransactionId, type, multisig, asset, revokePubKey,
                initialTr, clientAmount, hubAmount, lockedAddress, lockedScript);

            await _table.InsertAsync(entity);
            await _table.InsertAsync(CommitmentEntity.ByMonitoring.Create(entity));
            return entity;
        }

        public async Task<ICommitment> GetLastCommitment(string multisig, string asset, CommitmentType type)
        {
            var partition = CommitmentEntity.ByRecord.GeneratePartition(multisig, asset);
            var commitments = await _table.GetDataAsync(partition, o => o.Type == type);
            return commitments?.OrderByDescending(o => o.CreateDt).FirstOrDefault();
        }

        public async Task SetFullSignedTransaction(Guid commitmentId, string multisig, string asset, string fullSignedCommitment)
        {
            var partition = CommitmentEntity.ByRecord.GeneratePartition(multisig, asset);

            await _table.ReplaceAsync(partition, commitmentId.ToString(), entity =>
            {
                entity.SignedTransaction = fullSignedCommitment;
                return entity;
            });
        }

        public async Task<IEnumerable<ICommitment>> GetMonitoringCommitments()
        {
            var partition = CommitmentEntity.ByMonitoring.GeneratePartitionKey();
            return await _table.GetDataAsync(partition);
        }

        public async Task CloseCommitmentsOfChannel(string multisig, string asset, Guid channelId)
        {
            var partition = CommitmentEntity.ByRecord.GeneratePartition(multisig, asset);
            var commitments = await _table.GetDataAsync(partition, o => o.ChannelId == channelId);

            var tasks = new List<Task>();

            foreach (var commitment in commitments)
            {
                tasks.Add(_table.InsertAsync(CommitmentEntity.Archive.Create(commitment)));
                tasks.Add(_table.DeleteAsync(CommitmentEntity.ByMonitoring.GeneratePartitionKey(), commitment.CommitmentId.ToString()));
                tasks.Add(_table.DeleteAsync(commitment));
            }

            await Task.WhenAll(tasks);
        }

        public async Task<ICommitment> GetCommitment(string multisig, string asset, string transactionHex)
        {
            var partition = CommitmentEntity.ByRecord.GeneratePartition(multisig, asset);
            return (await _table.GetDataAsync(partition, o => TransactionComparer.CompareTransactions(o.InitialTransaction, transactionHex))).FirstOrDefault();

        }

        public async Task RemoveCommitmentsOfChannel(string multisig, string asset, Guid channelId)
        {
            var partition = CommitmentEntity.ByRecord.GeneratePartition(multisig, asset);
            var commitments = await _table.GetDataAsync(partition, o => o.ChannelId == channelId);
            foreach (var commitment in commitments)
            {
                await _table.DeleteAsync(CommitmentEntity.ByMonitoring.GeneratePartitionKey(), commitment.CommitmentId.ToString());
                await _table.DeleteAsync(partition, commitment.CommitmentId.ToString());
            }
        }
    }
}

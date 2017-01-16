using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Repositories;
using AzureStorage;
using Core.Repositories.FeeRate;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.FeeRate
{
    public class FeeRateEntity : TableEntity, IFeeRate
    {
        public static string GeneratePartitionKey()
        {
            return "FeeRate";
        }

        public static string GenerateRowKey()
        {
            return "FeeRate";
        }

        public int FeeRate { get; set; }

        public static FeeRateEntity Create(int feeRate)
        {
            return new FeeRateEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(),
                FeeRate = feeRate
            };
        }
    }

    public class FeeRateRepository : IFeeRateRepository
    {
        private const int MinTransactionFeePerByte = 80;

        private readonly INoSQLTableStorage<FeeRateEntity> _repository;

        public FeeRateRepository(INoSQLTableStorage<FeeRateEntity> repository)
        {
            _repository = repository;
        }

        public Task UpdateFeeRate(int fee)
        {
            return _repository.InsertOrReplaceAsync(FeeRateEntity.Create(fee));
        }

        public async Task<int> GetFeePerByte()
        {
            var result = await _repository.GetDataAsync(FeeRateEntity.GeneratePartitionKey(), FeeRateEntity.GenerateRowKey());
            return result?.FeeRate ?? MinTransactionFeePerByte;
        }
    }
}

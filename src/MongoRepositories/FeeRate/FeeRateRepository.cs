using System.Linq;
using System.Threading.Tasks;
using Core.Repositories.FeeRate;
using MongoRepositories.Mongo;

namespace MongoRepositories.FeeRate
{
    public class FeeRateEntity : MongoEntity, IFeeRate
    {
        public const string Id = "FeeRate";

        public int FeeRate { get; set; }

        public static FeeRateEntity Create(int feeRate)
        {
            return new FeeRateEntity
            {
                BsonId = Id,
                FeeRate = feeRate
            };
        }
    }

    public class FeeRateRepository : IFeeRateRepository
    {
        private const int MinTransactionFeePerByte = 80;

        private readonly IMongoStorage<FeeRateEntity> _repository;

        public FeeRateRepository(IMongoStorage<FeeRateEntity> repository)
        {
            _repository = repository;
        }

        public Task UpdateFeeRate(int fee)
        {
            return _repository.InsertOrReplaceAsync(FeeRateEntity.Create(fee));
        }

        public async Task<int> GetFeePerByte()
        {
            var result = await _repository.GetDataAsync(FeeRateEntity.Id);
            return result?.FeeRate ?? MinTransactionFeePerByte;
        }
    }
}

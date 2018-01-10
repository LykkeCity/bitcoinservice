using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Providers;
using Core.Repositories;
using Core.Repositories.FeeRate;
using Core.Settings;
using NBitcoin;
using BaseSettings = Core.Settings.BaseSettings;

namespace LkeServices.Providers
{
    public class FeeProvider : IFeeProvider
    {
        private readonly IFeeRateRepository _repository;
        private readonly BaseSettings _baseSettings;

        public FeeProvider(IFeeRateRepository repository, BaseSettings baseSettings)
        {
            _repository = repository;
            _baseSettings = baseSettings;
        }

        public async Task<Money> CalcFeeForTransaction(TransactionBuilder builder, decimal? feeMultiplier = null)
        {
            return builder.EstimateFees(builder.BuildTransaction(false), await GetFeeRate(feeMultiplier));
        }

        public async Task<Money> CalcFeeForTransaction(Transaction tr, int feeRate = 0)
        {
            var size = tr.ToBytes().Length;
            if (feeRate == 0)
                return await CalcFee(size);

            return new FeeRate(new Money(feeRate * 1000, MoneyUnit.Satoshi)).GetFee(size);
        }

        public async Task<FeeRate> GetFeeRate(decimal? feeMultiplier = null)
        {
            var feePerByte = await _repository.GetFeePerByte();

            // need fee per KB
            return new FeeRate(new Money(feePerByte * 1000 * feeMultiplier.GetValueOrDefault(_baseSettings.FeeRateMultiplier), MoneyUnit.Satoshi));
        }

        public async Task<Money> CalcFee(int size)
        {
            return (await GetFeeRate()).GetFee(size);
        }
    }
}

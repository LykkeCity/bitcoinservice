using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Performance;
using Core.QBitNinja;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace Core.Bitcoin
{

    public interface IBitcoinOutputsService
    {
        Task<IEnumerable<ICoin>> GetOnlyNinjaOutputs(string walletAddress, int minConfirmationsCount = 0, int maxConfirmationsCount = int.MaxValue);
        Task<IEnumerable<ICoin>> GetUnspentOutputs(string walletAddress, int confirmationsCount = 0, bool useInternalSpentOutputs = true, IPerformanceMonitor monitor = null);
        Task<IEnumerable<ICoin>> GetUncoloredUnspentOutputs(string walletAddress, int confirmationsCount = 0, bool useInternalSpentOutputs = true, IPerformanceMonitor monitor = null);
        Task<IEnumerable<ColoredCoin>> GetColoredUnspentOutputs(string walletAddress, AssetId assetIdObj, int confirmationsCount = 0, bool useInternalSpentOutputs = true, IPerformanceMonitor monitor = null);
        Task<IEnumerable<ColoredCoin>> GetColoredUnspentOutputs(string walletAddress, int confirmationsCount = 0, bool useInternalSpentOutputs = true, IPerformanceMonitor monitor = null);
    }
}

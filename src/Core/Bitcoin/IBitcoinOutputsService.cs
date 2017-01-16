using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.QBitNinja;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace Core.Bitcoin
{

    public interface IBitcoinOutputsService
    {
        Task<IEnumerable<ICoin>> GetUnspentOutputs(string walletAddress);
        Task<IEnumerable<ICoin>> GetUncoloredUnspentOutputs(string walletAddress);
        Task<IEnumerable<ColoredCoin>> GetColoredUnspentOutputs(string walletAddress, AssetId assetIdObj);
        Task<IEnumerable<ColoredCoin>> GetColoredUnspentOutputs(string walletAddress);
    }
}

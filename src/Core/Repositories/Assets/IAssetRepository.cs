﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Repositories.Assets
{
    public interface IAsset
    {
        string Id { get; }
        string BlockChainAssetId { get; }
        string AssetAddress { get; }
        int MultiplierPower { get; }
        string DefinitionUrl { get; }
        bool IsDisabled { get; }
        string PartnerId { get; }
        bool IssueAllowed { get; }
    }

    public interface IAssetRepository
    {
        Task<IAsset> GetAssetById(string id);
        Task<IEnumerable<IAsset>> GetBitcoinAssets();
    }
}

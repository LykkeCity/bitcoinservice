﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureRepositories.Assets;
using Common;
using Common.Log;
using Core;
using Core.Bitcoin;
using Core.Providers;
using Core.QBitNinja;
using Core.Repositories.Assets;
using Core.Repositories.Settings;
using Core.Repositories.Wallets;
using Core.Settings;
using LkeServices.Providers;
using LkeServices.Wallet;
using Lykke.JobTriggers.Triggers.Attributes;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace BitcoinJob.Functions
{
    public class AddNewChangeAddressFunction
    {
        private const int MaxTxCount = 5000;

        private readonly IQBitNinjaApiCaller _qBitNinjaApiCaller;
        private readonly CachedDataDictionary<string, IAssetSetting> _assetSettingCache;
        private readonly CachedDataDictionary<string, IAsset> _assetRepository;
        private readonly RpcConnectionParams _connectionParams;
        private readonly IWalletService _walletService;        
        private readonly ISettingsRepository _settingsRepository;
        private readonly ISignatureApiProvider _signatureApiProvider;
        private readonly BaseSettings _baseSettings;
        private readonly IAssetSettingRepository _assetSettingRepository;
        private readonly ILog _logger;


        public AddNewChangeAddressFunction(IQBitNinjaApiCaller qBitNinjaApiCaller, 
            CachedDataDictionary<string, IAssetSetting> assetSettingCache, ISettingsRepository settingsRepository, 
            ISignatureApiProvider signatureApiProvider, BaseSettings baseSettings,
            IAssetSettingRepository assetSettingRepository, ILog logger, CachedDataDictionary<string, IAsset> assetRepository,
            RpcConnectionParams connectionParams,IWalletService walletService)
        {
            _qBitNinjaApiCaller = qBitNinjaApiCaller;
            _assetSettingCache = assetSettingCache;
            _settingsRepository = settingsRepository;
            _baseSettings = baseSettings;
            _assetSettingRepository = assetSettingRepository;
            _logger = logger;
            _assetRepository = assetRepository;
            _connectionParams = connectionParams;
            _walletService = walletService;            
            _signatureApiProvider = signatureApiProvider;
        }

        [TimerTrigger("00:30:00")]
        public async Task Process()
        {
            var maxTxCount = await _settingsRepository.Get(Constants.MaxOffchainTxCount, MaxTxCount);
            var assets = await _assetRepository.Values();

            foreach (var asset in assets)
            {
                var setting = await GetAssetSetting(asset.Id);

                if (setting.HotWallet != setting.ChangeWallet)
                    continue;

                var summary = await _qBitNinjaApiCaller.GetBalanceSummary(setting.HotWallet);

                if (summary.Confirmed.TransactionCount < maxTxCount)
                    continue;

                await _logger.WriteInfoAsync(nameof(AddNewChangeAddressFunction), nameof(Process), $"Asset: {asset.Id}", "Start generating new change wallet");

                var increment = await GetNewIncrement();

                if (setting.Asset == Constants.DefaultAssetSetting)
                {
                    await _logger.WriteInfoAsync(nameof(AddNewChangeAddressFunction), nameof(Process), $"Asset: {asset.Id}", "Create new asset setting");

                    var newSetting = setting.Clone(asset.Id);

                    newSetting.ChangeWallet = increment.CurrentAddress;
                    newSetting.PrivateIncrement = increment.CurrentIncrement;

                    await _assetSettingRepository.Insert(newSetting);

                }
                else
                {
                    await _assetSettingRepository.UpdateChangeAndIncrement(setting.Asset, increment.CurrentAddress, increment.CurrentIncrement);
                }

                await _logger.WriteInfoAsync(nameof(AddNewChangeAddressFunction), nameof(Process), $"Asset: {asset.Id}, new change: {increment.CurrentAddress}", "Finish generating new change wallet");
            }
        }

        private async Task<Increment> GetNewIncrement()
        {
            var currentPrivateIncrementSetting = await _settingsRepository.Get<string>(Constants.CurrentPrivateIncrementSetting);

            Increment currentIncrement;

            async Task<string> GetNextAddress(string addr)
            {                
                return (await _walletService.CreateSegwitWallet(await _signatureApiProvider.GetNextAddress(addr))).Address;                
            }

            if (currentPrivateIncrementSetting == null)
            {
                currentIncrement = new Increment
                {
                    CurrentAddress = await GetNextAddress(_baseSettings.Offchain.HotWallet),
                    CurrentIncrement = 1
                };
            }
            else
            {
                currentIncrement = Newtonsoft.Json.JsonConvert.DeserializeObject<Increment>(currentPrivateIncrementSetting);                
                currentIncrement.CurrentAddress = await GetNextAddress(currentIncrement.CurrentAddress);
                currentIncrement.CurrentIncrement++;
            }

            await _settingsRepository.Set(Constants.CurrentPrivateIncrementSetting, currentIncrement.ToJson());
            return currentIncrement;
        }

        private async Task<IAssetSetting> GetAssetSetting(string asset)
        {
            var setting = await _assetSettingCache.GetItemAsync(asset) ??
                          await _assetSettingCache.GetItemAsync(Constants.DefaultAssetSetting);
            if (setting == null)
                throw new Exception($"Setting is not found for {asset}");
            return setting;
        }

        public class Increment
        {
            public string CurrentAddress { get; set; }
            public int CurrentIncrement { get; set; }
        }
    }
}

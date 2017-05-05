using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.Wallets;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.Walelts
{
    public class WalletAddressEntity : TableEntity, IWalletAddress
    {
        public string ClientPubKey { get; set; }
        public string ExchangePubKey { get; set; }
        public string MultisigAddress { get; set; }
        public string RedeemScript { get; set; }

        public static class ByMultisig
        {
            public static string GeneratePartitionKey()
            {
                return "ByMultisig";
            }

            public static WalletAddressEntity Create(string multisig, string clientPubKey, string exchangePubKey, string redeemScript)
            {
                return new WalletAddressEntity
                {
                    PartitionKey = GeneratePartitionKey(),
                    RowKey = multisig,
                    MultisigAddress = multisig,
                    RedeemScript = redeemScript,
                    ClientPubKey = clientPubKey,
                    ExchangePubKey = exchangePubKey
                };
            }
        }

        public static class ByClientPubKey
        {
            public static string GeneratePartitionKey()
            {
                return "ByClientPubKey";
            }

            public static WalletAddressEntity Create(string multisig, string clientPubKey, string exchangePubKey, string redeemScript)
            {
                return new WalletAddressEntity
                {
                    PartitionKey = GeneratePartitionKey(),
                    RowKey = clientPubKey,
                    MultisigAddress = multisig,
                    RedeemScript = redeemScript,
                    ClientPubKey = clientPubKey,
                    ExchangePubKey = exchangePubKey
                };
            }

        }
    }

    public class WalletAddressRepository : IWalletAddressRepository
    {
        private readonly INoSQLTableStorage<WalletAddressEntity> _storage;

        public WalletAddressRepository(INoSQLTableStorage<WalletAddressEntity> storage)
        {
            _storage = storage;
        }

        public async Task<IWalletAddress> Create(string multisig, string clientPubKey, string exchangePubKey, string redeemScript)
        {
            var rec = WalletAddressEntity.ByClientPubKey.Create(multisig, clientPubKey, exchangePubKey, redeemScript);
            await _storage.InsertAsync(WalletAddressEntity.ByMultisig.Create(multisig, clientPubKey, exchangePubKey, redeemScript));
            await _storage.InsertAsync(rec);
            return rec;
        }

        public async Task<string> GetRedeemScript(string multisigAdress)
        {
            var data = await _storage.GetDataAsync(WalletAddressEntity.ByMultisig.GeneratePartitionKey(), multisigAdress);
            return data.RedeemScript;
        }

        public async Task<IWalletAddress> GetByClientPubKey(string clientPubKey)
        {
            return await _storage.GetDataAsync(WalletAddressEntity.ByClientPubKey.GeneratePartitionKey(), clientPubKey);
        }

        public async Task<IEnumerable<IWalletAddress>> GetAllAddresses()
        {
            return await _storage.GetDataAsync(WalletAddressEntity.ByMultisig.GeneratePartitionKey());
        }
    }
}

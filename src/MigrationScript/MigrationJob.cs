using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Core.Repositories.Wallets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoRepositories.Utils;
using NBitcoin;
using NBitcoin.BuilderExtensions;

namespace MigrationScript
{
    public class MigrationJob
    {
        private readonly IWalletAddressRepository _azureWallets;
        private readonly IWalletAddressRepository _mongoWallets;

        public MigrationJob(Func<string, IWalletAddressRepository> factory, IConfiguration configuration)
        {
            _azureWallets = factory("azure");
            _mongoWallets = factory("mongo");
        }

        public async Task Start()
        {
            var records = await _azureWallets.GetAllAddresses();
            foreach (var walletAddress in records)
            {
                try
                {
                    await _mongoWallets.Create(walletAddress.MultisigAddress, walletAddress.ClientPubKey, walletAddress.ExchangePubKey,
                                walletAddress.RedeemScript);
                }
                catch (MongoWriteException ex)
                {
                    if (!ex.IsDuplicateError())
                        throw;
                }
            }
        }
    }
}

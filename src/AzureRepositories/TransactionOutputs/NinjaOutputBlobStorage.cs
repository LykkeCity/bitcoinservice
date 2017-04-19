using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureStorage;
using Core.Repositories.TransactionOutputs;
using NBitcoin;
using QBitNinja.Client;

namespace AzureRepositories.TransactionOutputs
{
    public class NinjaOutputBlobStorage : INinjaOutputBlobStorage
    {
        private const string BlobContainer = "ninja-outputs";

        private readonly IBlobStorage _storage;

        public NinjaOutputBlobStorage(IBlobStorage storage)
        {
            _storage = storage;
        }

        public async Task Save(string address, IEnumerable<ICoin> coins)
        {
            try
            {
                var str = Serializer.ToString(coins, Base58Data.GetFromBase58Data(address).Network);

                await _storage.SaveBlobAsync(BlobContainer, $"{address}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss.fff}.txt", Encoding.UTF8.GetBytes(str));
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
                
            }
        }
    }
}

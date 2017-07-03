using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Microsoft.Extensions.Configuration;
using OffchainRequestCreator.Repositories;

namespace PoisonMessagesReenqueue
{
    public class Job
    {
        private readonly IOffchainRequestRepository _offchainRequestRepository;
        private readonly IOffchainTransferRepository _offchainTransferRepository;

        public Job(IOffchainRequestRepository offchainRequestRepository, IOffchainTransferRepository offchainTransferRepository)
        {
            _offchainRequestRepository = offchainRequestRepository;
            _offchainTransferRepository = offchainTransferRepository;
        }

        public async Task Start()
        {
            //await GetStatistics(DateTime.UtcNow.AddDays(-10), DateTime.UtcNow);
        }

        private async Task AddRequest(string client, string asset, decimal amount,
            OffchainTransferType type = OffchainTransferType.FromHub, string order = null)
        {
            var guid = Guid.NewGuid().ToString();

            var transfer = await _offchainTransferRepository.CreateTransfer(guid, client, asset, amount, type, null, order);

            await _offchainRequestRepository.CreateRequest(transfer.Id, client, asset, RequestType.RequestTransfer, type);
        }

        private async Task GetStatistics(DateTime from, DateTime to)
        {
            var trasnfers = (await _offchainTransferRepository.GetTransfers(from, to)).Where(x => x.Completed);

            var byType = trasnfers.GroupBy(x => x.Type);

            foreach (var type in byType)
            {
                var onchainCount = type.Count(x => x.Onchain);
                var offchainCount = type.Count(x => !x.Onchain);

                //FromHub --> SettlementFromHub
                //FromClient-- > SettlementFromClient

                WriteLine(type.Key.ToString());
                WriteLine($"Onchain: {onchainCount}, offchain: {offchainCount}");
                WriteLine("");

                var byAsset = type.GroupBy(x => x.AssetId);

                foreach (var asset in byAsset)
                {
                    WriteLine($"     {string.Format("{0, -5}", asset.Key)} - onchain: {asset.Count(x => x.Onchain)}, offchain: {asset.Count(x => !x.Onchain)}");
                }

                WriteLine("");
            }
        }

        private void WriteLine(string str)
        {
            File.AppendAllLines("output.txt", new[] { str });
        }
    }
}

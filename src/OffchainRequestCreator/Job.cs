using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Common;
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
            //await GetStatistics(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow);


            var transafers = await _offchainTransferRepository.GetTransfersByDate(OffchainTransferType.FromHub, DateTime.UtcNow.AddHours(-16),
                DateTime.UtcNow);

            var groupped = transafers.Where(x => !x.IsChild).GroupBy(x => x.OrderId).ToDictionary(x => x.Key, x => x.Count())
                .OrderByDescending(x => x.Value);

            var values = groupped.Select(x => x.Value);

            var av = values.Average();

        }

        private async Task AggregateRequests(string clientId, string asset, OffchainTransferType type)
        {
            var list = (await _offchainRequestRepository.GetRequestsForClient(clientId)).Where(x => x.AssetId == asset && x.TransferType == type).ToList();

            var masterRequest = list.FirstOrDefault(x =>
                (x.StartProcessing == null || (DateTime.UtcNow - x.StartProcessing.Value).TotalMinutes > 5) && x.ServerLock == null);

            if (masterRequest == null)
                return;

            await _offchainRequestRepository.DeleteRequest(masterRequest.RequestId);

            LogToFile(masterRequest.ToJson());

            var masterTransfer = await _offchainTransferRepository.GetTransfer(masterRequest.TransferId);

            int count = 0;
            while (count < 50)
            {
                list = (await _offchainRequestRepository.GetRequestsForClient(clientId))
                    .Where(x => (x.StartProcessing == null || (DateTime.UtcNow - x.StartProcessing.Value).TotalMinutes > 5) && x.ServerLock == null)
                    .Where(x => x.AssetId == asset && x.RequestId != masterRequest.RequestId && x.TransferType == type).ToList();
                if (list.Count < 1)
                    break;

                var current = list.FirstOrDefault();

                await _offchainRequestRepository.DeleteRequest(current.RequestId);

                LogToFile(current.ToJson());

                var currentTransfer = await _offchainTransferRepository.GetTransfer(current.TransferId);

                await _offchainTransferRepository.SetTransferIsChild(currentTransfer.Id, masterTransfer.Id);

                await _offchainTransferRepository.AddChildTransfer(masterTransfer.Id, currentTransfer);

                count++;
            }

            await _offchainRequestRepository.CreateRequest(masterTransfer.Id, masterTransfer.ClientId,
                masterTransfer.AssetId, masterRequest.Type, masterTransfer.Type);
        }

        private void LogToFile(string msg)
        {
            const string fileName = "AggregateRequests.txt";

            File.AppendAllLines(fileName, new[] { msg });
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

            WriteLine($"Operation Settlement Asset Number");

            foreach (var type in byType)
            {
                var onchainCount = type.Count(x => x.Onchain);
                var offchainCount = type.Count(x => !x.Onchain);

                string typeName;
                switch (type.Key)
                {
                    case OffchainTransferType.FromHub:
                        typeName = "SettlementFromHub";
                        break;
                    case OffchainTransferType.FromClient:
                        typeName = "SettlementFromClient";
                        break;
                    default:
                        typeName = type.Key.ToString();
                        break;
                }

                var byAsset = type.GroupBy(x => x.AssetId);

                foreach (var asset in byAsset)
                {
                    WriteLine($"{typeName} onchain {asset.Key} {asset.Count(x => x.Onchain)}");
                }

                foreach (var asset in byAsset)
                {
                    WriteLine($"{typeName} offchain {asset.Key} {asset.Count(x => !x.Onchain)}");
                }
            }
        }

        private void WriteLine(string str)
        {
            File.AppendAllLines("output.txt", new[] { str });
        }
    }
}

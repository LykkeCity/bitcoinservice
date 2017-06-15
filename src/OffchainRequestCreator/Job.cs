using System;
using System.Collections.Generic;
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

        }

        private async Task AddRequest(string client, string asset, decimal amount,
            OffchainTransferType type = OffchainTransferType.FromHub, string order = null)
        {
            var guid = Guid.NewGuid().ToString();

            var transfer = await _offchainTransferRepository.CreateTransfer(guid, client, asset, amount, type, null, order);

            await _offchainRequestRepository.CreateRequest(transfer.Id, client, asset, RequestType.RequestTransfer, type);
        }
    }
}

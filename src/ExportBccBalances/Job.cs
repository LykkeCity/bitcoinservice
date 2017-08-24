using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Repositories.Offchain;
using Microsoft.Extensions.Configuration;
using NBitcoin;

namespace PoisonMessagesReenqueue
{
    public class Job
    {
        private readonly IOffchainChannelRepository _offchainChannelRepository;
        private readonly ICommitmentRepository _commitmentRepository;

        public Job(IOffchainChannelRepository offchainChannelRepository, ICommitmentRepository commitmentRepository)
        {
            _offchainChannelRepository = offchainChannelRepository;
            _commitmentRepository = commitmentRepository;
        }

        public async Task Start()
        {
           


            using (var file = File.Create("output.csv"))
            {

                using (var fileWriter = new StreamWriter(file))
                {

                    await fileWriter.WriteLineAsync("Multisig;Client;Hub");

                    var allChannels = await _offchainChannelRepository.GetAllChannels("BTC");

                    foreach (var channels in allChannels.GroupBy(o => o.Multisig))
                    {
                        Money clientAmount = Money.Zero;
                        Money hubAmount = Money.Zero;

                        var channel = channels.Where(o => o.CreateDt <= Constants.PrevBccBlockTime && o.IsBroadcasted &&
                                                          (o.Actual || o.BsonCreateDt > Constants.PrevBccBlockTime))
                            .OrderByDescending(o => o.CreateDt).FirstOrDefault();
                        if (channel != null)
                        {
                            var commitments = await _commitmentRepository.GetCommitments(channel.ChannelId);
                            var commitment = commitments.Where(o => o.Type == CommitmentType.Client && o.CreateDt < Constants.PrevBccBlockTime)
                                .OrderByDescending(o => o.CreateDt).FirstOrDefault();
                            clientAmount = Money.FromUnit(commitment.ClientAmount, MoneyUnit.BTC);
                            hubAmount = Money.FromUnit(commitment.HubAmount, MoneyUnit.BTC);
                            await fileWriter.WriteLineAsync($"{channels.Key};{clientAmount.ToDecimal(MoneyUnit.BTC)};{hubAmount.ToDecimal(MoneyUnit.BTC)}");
                        }                        
                    }
                }
            }
        }

    }
}

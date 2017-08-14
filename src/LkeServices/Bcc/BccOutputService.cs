using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac.Features.AttributeFilters;
using Core;
using Core.Bcc;
using Core.Outputs;
using Core.Performance;
using Core.QBitNinja;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Wallets;
using NBitcoin;
using NBitcoin.OpenAsset;

namespace LkeServices.Bcc
{
    public class BccOutputService : IBccOutputService
    {
        private readonly IBccQbBitNinjaApiCaller _qbitNinjaApiCaller;
        private readonly ISpentOutputRepository _spentOutputRepository;
        private readonly IWalletAddressRepository _walletAddressRepository;

        public BccOutputService(IBccQbBitNinjaApiCaller qbitNinjaApiCaller, [KeyFilter(Constants.BccKey)] ISpentOutputRepository spentOutputRepository,
            IWalletAddressRepository walletAddressRepository)
        {
            _qbitNinjaApiCaller = qbitNinjaApiCaller;
            _spentOutputRepository = spentOutputRepository;
            _walletAddressRepository = walletAddressRepository;
        }

        public async Task<IEnumerable<ICoin>> GetUnspentOutputs(string walletAddress)
        {
            var outputResponse = await _qbitNinjaApiCaller.GetAddressBalance(walletAddress);
            var coins = outputResponse.Operations
                .Where(x => x.Confirmations >= 1)
                .SelectMany(o => o.ReceivedCoins).ToList();

            coins = await FilterCoins(coins);

            return await ToScriptCoins(walletAddress, coins);
        }


        private async Task<IEnumerable<ICoin>> ToScriptCoins(string walletAddress, List<ICoin> coins)
        {
            var address = BitcoinAddress.Create(walletAddress);
            if (address is BitcoinScriptAddress)
            {
                var redeem = await _walletAddressRepository.GetRedeemScript(walletAddress);
                return coins.OfType<Coin>().Select(x => new ScriptCoin(x, new Script(redeem)))
                    .Concat(
                        coins.OfType<ColoredCoin>().Select(x => new ScriptCoin(x, new Script(redeem)).ToColoredCoin(x.Amount))
                            .Cast<ICoin>());
            }
            return coins;
        }

        private async Task<List<ICoin>> FilterCoins(List<ICoin> coins)
        {
            var unspentOutputs = await _spentOutputRepository.GetUnspentOutputs(coins.Select(o => new Output(o.Outpoint)));

            var unspentSet = new HashSet<OutPoint>(unspentOutputs.Select(x => new OutPoint(uint256.Parse(x.TransactionHash), x.N)));

            return coins.Where(o => unspentSet.Contains(o.Outpoint)).ToList();
        }
    }
}

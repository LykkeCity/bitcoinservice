using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Enums;
using Core.Exceptions;
using Core.OpenAssets;
using Core.Repositories.Wallets;
using Core.Settings;
using LkeServices.Bitcoin;
using LkeServices.QBitNinja;
using LkeServices.Transactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoRepositories.Mongo;
using MongoRepositories.TransactionOutputs;
using Moq;
using NBitcoin;
using QBitNinja.Client;

namespace tests
{
    [TestClass]
    public class FeeTest
    {
        [TestMethod]
        public async Task TestFee()
        {
            BitcoinAddress source = BitcoinAddress.Create("1Ppn1SfPw34s99GoZNysey6R7hHLKbSEQn", Network.Main);
            BitcoinAddress destinationAddress = BitcoinAddress.Create("35YKYNqYsu2RJA2EjcoYmTgTVG2atd2mzB", Network.Main);
            decimal amount = 0.00091294m;
            decimal fee = 0.0000273m;
            Guid transactionId = Guid.Parse("392461b2-3a5b-491d-bb2b-74af7acf4400");

            var origTransactionHex =
                "0100000001b7b9530a1e0069b2ef41e0aca73f66ed87f63b7ec8b6b1c1be8f3a6bb2d1e9e8010000006b483045022100a6f33894b337a77ade9e99a8333be78b40719bf88fa9ef66c0ff91e4f6c7b70f02207124867503bcff718967a02d3a842443b517832f777466fd0bd148d5dc7accfb012102f9444cc3b2ed9c1bb2e298f44ae0f72da73363d622f7ca1066c9b6e1309b42b0ffffffff02b5cd2400000000001976a914fa5be34ef225055144dc3ad58cc63982fb65133e88ac74e300000000000017a9142a3b7751f97114a4a3f3815343db4a05bd20e6548700000000";

            var tr = Transaction.Parse(origTransactionHex);
            var coins = new List<Coin>
            {
                new Coin(uint256.Parse("e8e9d1b26b3a8fbec1b1b6c87e3bf687ed663fa7ace041efb269001e0a53b9b7"), 1,
                    new Money(0.02503247m, MoneyUnit.BTC),
                    new Script("76a914fa5be34ef225055144dc3ad58cc63982fb65133e88ac"))
            };

            var totalAmount = new Money(coins.Select(o => o.Amount).DefaultIfEmpty().Sum(o => o?.Satoshi ?? 0));

            if (totalAmount.ToDecimal(MoneyUnit.BTC) < amount)
                throw new BackendException($"The sum of total applicable outputs is less than the required: {amount} btc.", ErrorCode.NotEnoughBitcoinAvailable);

            var builder = new TransactionBuilder();
            builder.AddCoins(coins);
            builder.SetChange(source);
            builder.Send(destinationAddress, new Money(amount, MoneyUnit.BTC));
            builder.SubtractFees();

            builder.SendFees(new Money(fee, MoneyUnit.BTC));

            var tx = builder.BuildTransaction(true);

            var x = new PrivateTransferResponse(tx.ToHex(), transactionId, fee);

        }

    }
}

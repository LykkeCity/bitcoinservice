﻿using System.Threading.Tasks;
using Lykke.Bitcoin.Api.Client.AutoGenerated;
using Lykke.Bitcoin.Api.Client.AutoGenerated.Models;
using Lykke.Bitcoin.Api.Client.BitcoinApi.Models;

// ReSharper disable once CheckNamespace
namespace Lykke.Bitcoin.Api.Client.BitcoinApi
{
    public partial class BitcoinApiClient
    {
        public async Task<BccSplitTransactionResponse> BccSplitTransaction(string multisig, string clientDestination, string hubDestination)
        {
            var response = await _apiClient.ApiBccSplitGetAsync(multisig, clientDestination, hubDestination);

            return PrepareResult(response, o =>
            {
                if (response is SplitTransactionResponse model)
                    return new BccSplitTransactionResponse
                    {
                        Transaction = model.Transaction,
                        ClientAmount = model.ClientAmount ?? 0,
                        HubAmount = model.HubAmount ?? 0,
                        ClientFeeAmount = model.ClientFeeAmount ?? 0,
                        Outputs = model.Outputs
                    };
                return null;
            });
        }

        public async Task<BccTransactionResponse> BccPrivateTransferTransaction(string sourceAddress, string destinationAddress, decimal fee)
        {
            var response = await _apiClient.ApiBccPrivatetransferGetAsync(sourceAddress, destinationAddress, fee);

            return PrepareResult(response, o =>
            {
                if (response is PrivateBccTransferResponse model)
                    return new BccTransactionResponse
                    {
                        Transaction = model.Transaction,
                        Outputs = model.Outputs
                    };
                return null;
            });
        }

        public async Task<BccTransactionHashResponse> BccBroadcast(string transaction)
        {
            var response = await _apiClient.ApiBccBroadcastPostAsync(new BccBroadcastModel(transaction));


            return PrepareResult(response, o =>
            {
                if (response is TransactionHashResponse model)
                    return new BccTransactionHashResponse
                    {
                        TransactionHash = model.TransactionHash
                    };
                return null;
            });
        }

        public async Task<Models.BccBalanceResponse> BccBalance(string address)
        {
            var response = await _apiClient.ApiBccBalanceGetAsync(address);
            return PrepareResult(response, o =>
            {
                if (o is AutoGenerated.Models.BccBalanceResponse model)
                    return new Models.BccBalanceResponse
                    {
                        Balance = model.Balance ?? 0
                    };
                return null;
            });
        }
    }
}

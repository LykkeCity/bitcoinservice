﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Bitcoin.Api.Client.AutoGenerated;
using Lykke.Bitcoin.Api.Client.AutoGenerated.Models;
using Lykke.Bitcoin.Api.Client.BitcoinApi.Models;
using TransactionResponse = Lykke.Bitcoin.Api.Client.BitcoinApi.Models.TransactionResponse;

// ReSharper disable once CheckNamespace
namespace Lykke.Bitcoin.Api.Client.BitcoinApi
{
    public partial class BitcoinApiClient
    {
        public async Task<OnchainResponse> TransactionTransfer(Guid? transactionId, string sourceAddress, string destinationAddress, decimal amount, string asset)
        {
            var response = await _apiClient.ApiTransactionTransferPostAsync(new TransferRequest
            {
                TransactionId = transactionId,
                Amount = amount,
                Asset = asset,
                DestinationAddress = destinationAddress,
                SourceAddress = sourceAddress
            });

            return PrepareResult(response, o =>
            {
                if (response is Lykke.Bitcoin.Api.Client.AutoGenerated.Models.TransactionResponse model)
                    return new OnchainResponse
                    {
                        Transaction = new TransactionResponse
                        {
                            Transaction = model.Transaction,
                            TransactionId = model.TransactionId
                        }
                    };
                return null;
            });
        }

        public Task TransactionBroadcast(Guid transactionId, string transaction)
        {
            return _apiClient.ApiTransactionBroadcastPostAsync(new BroadcastTransactionRequest
            {
                TransactionId = transactionId,
                Transaction = transaction
            });
        }

        public async Task<OnchainResponse> TransactionMultipleTransfer(Guid? transactionId, string destination, string asset, int feeRate, IEnumerable<ToOneAddress> sources)
        {
            var response = await _apiClient.ApiTransactionMultipletransferPostAsync(new MultipleTransferRequest
            {
                TransactionId = transactionId,
                Asset = asset,
                Destination = destination,
                FeeRate = feeRate,
                Sources = sources.ToList()
            });

            return PrepareResult(response, o =>
            {
                if (response is TransactionIdAndHashResponse model)
                    return new OnchainResponse
                    {
                        Transaction = new TransactionResponse
                        {
                            Hash = model.Hash,
                            TransactionId = model.TransactionId
                        }
                    };
                return null;
            });
        }

        public async Task<OnchainResponse> TransactionGetById(Guid transactionId)
        {
            var response = await _apiClient.ApiTransactionByTransactionIdGetAsync(transactionId);

            return PrepareResult(response, o =>
            {
                if (response is TransactionHashResponse model)
                    return new OnchainResponse
                    {
                        Transaction = new TransactionResponse
                        {
                            Hash = model.TransactionHash
                        }
                    };
                return null;
            });
        }

      
    }
}
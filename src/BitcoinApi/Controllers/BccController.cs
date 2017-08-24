using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BitcoinApi.Filters;
using BitcoinApi.Models.Bcc;
using BitcoinApi.Models.Offchain;
using Core.OpenAssets;
using LkeServices.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace BitcoinApi.Controllers
{
    [Route("api/[controller]")]
    public class BccController : Controller
    {
        private readonly IBccTransactionService _bccTransactionService;

        public BccController(IBccTransactionService bccTransactionService)
        {
            _bccTransactionService = bccTransactionService;
        }

        [HttpGet("split")]
        [ProducesResponseType(typeof(SplitTransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<SplitTransactionResponse> GetSplitTransaction([FromQuery]string multisig, [FromQuery]string clientDestination, [FromQuery]string hubDestination)
        {
            var result = await _bccTransactionService.CreateSplitTransaction(multisig, OpenAssetsHelper.GetBitcoinAddressFormBase58Date(clientDestination),
                OpenAssetsHelper.GetBitcoinAddressFormBase58Date(hubDestination));
            return new SplitTransactionResponse(result);
        }

        [HttpGet("broadcast")]
        [ProducesResponseType(typeof(TransactionHashResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<TransactionHashResponse> Broadcast([FromBody]BccBroadcastModel model)
        {
            return new TransactionHashResponse(await _bccTransactionService.Broadcast(model.Transaction, null));
        }

        [HttpGet("privatetransfer")]
        [ProducesResponseType(typeof(PrivateBccTransferResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<PrivateBccTransferResponse> GetPrivateTransfer([FromQuery]string sourceAddress, [FromQuery]string destinationAddress, [FromQuery]decimal fee)
        {
            return new PrivateBccTransferResponse
            {
                Transaction = await _bccTransactionService.CreatePrivateTransfer(OpenAssetsHelper.GetBitcoinAddressFormBase58Date(sourceAddress),
                                                                                 OpenAssetsHelper.GetBitcoinAddressFormBase58Date(destinationAddress), fee)
            };
        }
    }
}

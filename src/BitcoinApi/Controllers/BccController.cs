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
            var result = await _bccTransactionService.CreateSplitTransaction(multisig, OpenAssetsHelper.ParseAddress(clientDestination),
                OpenAssetsHelper.ParseAddress(hubDestination));
            return new SplitTransactionResponse(result);
        }

        [HttpPost("broadcast")]
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
            var result = await _bccTransactionService.CreatePrivateTransfer(OpenAssetsHelper.ParseAddress(sourceAddress),
                OpenAssetsHelper.ParseAddress(destinationAddress), fee);
            return new PrivateBccTransferResponse
            {
                Transaction = result.TransactionHex,
                Outputs = result.Outputs
            }; 
        }
        [HttpGet("balance")]
        [ProducesResponseType(typeof(BccBalanceResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<BccBalanceResponse> GetBccBalance([FromQuery] string address)
        {
            var balance = await _bccTransactionService.GetAddressBalance(address);
            return new BccBalanceResponse
            {
                Balance = balance
            };
        }
    }
}

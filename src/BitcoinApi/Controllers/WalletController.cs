﻿using System;
using System.Linq;
using System.Threading.Tasks;
using BitcoinApi.Filters;
using BitcoinApi.Models;
using Core;
using Core.Bitcoin;
using Core.Providers;
using LkeServices.Providers;
using LkeServices.Wallet;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;

namespace BitcoinApi.Controllers
{
    [Route("api/[controller]")]
    public class WalletController : Controller
    {
        private readonly IWalletService _walletService;
        private readonly ISignatureApiProvider _signatureProvider;

        public WalletController(IWalletService walletService, ISignatureApiProvider signatureApiProvider)
        {
            _walletService = walletService;
            _signatureProvider = signatureApiProvider;
        }

        /// <summary>
        /// Returns 2-of-2 multisig with exchange key and provided public key
        /// </summary>
        /// <param name="clientPubKey">Client public key</param>
        /// <remarks>
        /// curl -X GET http://localhost:8989/api/wallet/&lt;client_public_key&gt; 
        /// </remarks>
        /// <returns>Multisig address and colored (OpenAssets) representation</returns>
        [HttpGet("{clientPubKey}")]
        [ProducesResponseType(typeof(GetWalletResult), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<GetWalletResult> GetWallet(string clientPubKey)
        {
            var address = await _walletService.GetOrCreateMultisig(clientPubKey);

            var coloredMultisigAddress = BitcoinAddress.Create(address.MultisigAddress).ToColoredAddress().ToString();

            return new GetWalletResult
            {
                MultiSigAddress = address.MultisigAddress,
                ColoredMultiSigAddress = coloredMultisigAddress
            };
        }

        [HttpGet("segwit/{clientPubKey}")]
        [ProducesResponseType(typeof(SegwitWalletResult), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<SegwitWalletResult> GetSegwitWallet(string clientPubKey)
        {
            var address = await _walletService.GetOrCreateSegwitPrivateWallet(clientPubKey);

            var coloredSegwit = BitcoinAddress.Create(address.Address).ToColoredAddress().ToString();

            return new SegwitWalletResult
            {
                SegwitAddress = address.Address,
                ColoredSegwitAddress = coloredSegwit
            };
        }

        [HttpGet("segwit")]
        [ProducesResponseType(typeof(SegwitWalletResult), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<SegwitWalletResult> GetSegwitWallet()
        {
            var address = await _walletService.CreateSegwitWallet();

            var coloredSegwit = BitcoinAddress.Create(address.Address).ToColoredAddress().ToString();

            return new SegwitWalletResult
            {
                SegwitAddress = address.Address,
                ColoredSegwitAddress = coloredSegwit
            };
        }

        /// <summary>
        /// Returns all registered multisigs
        /// </summary>
        /// <returns>Array with all multisigs</returns>
        [HttpGet("all")]
        [ProducesResponseType(typeof(GetAllWalletsResult), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<GetAllWalletsResult> GetAllWallets()
        {
            var data = await _walletService.GetAllMultisigs();

            return new GetAllWalletsResult
            {
                Multisigs = data.Select(x => x.MultisigAddress)
            };
        }

        [HttpPost("lykkepay/generate")]
        [ProducesResponseType(typeof(GenerateWalletResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<GenerateWalletResponse> GenerateLykkePayWallet()
        {
            var response = await _signatureProvider.GenerateWallet(Constants.LykkePayTag);
            return new GenerateWalletResponse
            {
                Address = response.Address,
                PubKey = response.PubKey,
                Tag = Constants.LykkePayTag
            };
        }
    }
}

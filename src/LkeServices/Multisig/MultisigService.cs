using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Bitcoin;
using Core.Helpers;
using Core.Providers;
using Core.Repositories.Wallets;

namespace LkeServices.Multisig
{
    public interface IMultisigService
    {
        Task<IWalletAddress> GetOrCreateMultisig(string clientPubKey);
        Task<IWalletAddress> GetMultisig(string clientPubKey);
    }

    public class MultisigService : IMultisigService
    {
        private readonly IWalletAddressRepository _walletAddressRepository;
        private readonly ISignatureApiProvider _signatureApiProvider;
        private readonly RpcConnectionParams _connectionParams;

        public MultisigService(IWalletAddressRepository walletAddressRepository,
            ISignatureApiProvider signatureApiProvider,
            RpcConnectionParams connectionParams)
        {
            _walletAddressRepository = walletAddressRepository;
            _signatureApiProvider = signatureApiProvider;
            _connectionParams = connectionParams;
        }
        
        public async Task<IWalletAddress> GetOrCreateMultisig(string clientPubKey)
        {
            var multisig = await _walletAddressRepository.GetByClientPubKey(clientPubKey);
            if (multisig != null)
                return multisig;

            var exchangePubKey = await _signatureApiProvider.GeneratePubKey();
            return await CreateMultisig(clientPubKey, exchangePubKey);
        }

        public async Task<IWalletAddress> GetMultisig(string clientPubKey)
        {
            return await _walletAddressRepository.GetByClientPubKey(clientPubKey);
        }

        private async Task<IWalletAddress> CreateMultisig(string clientPubKey, string exchangePubKey)
        {
            var scriptPubKey = MultisigHelper.GenerateMultisigRedeemScript(clientPubKey, exchangePubKey);
            return await _walletAddressRepository.Create(scriptPubKey.GetScriptAddress(_connectionParams.Network).ToWif(),
                clientPubKey, exchangePubKey, scriptPubKey.ToString());
        }
    }
}

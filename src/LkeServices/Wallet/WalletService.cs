using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Helpers;
using Core.Providers;
using Core.Repositories.Wallets;
using NBitcoin;
using RpcConnectionParams = Core.Settings.RpcConnectionParams;

namespace LkeServices.Wallet
{
    public interface IWalletService
    {
        Task<IWalletAddress> GetOrCreateMultisig(string clientPubKey);
        Task<IWalletAddress> GetMultisig(string clientPubKey);
        Task<IWalletAddress> GetMultisigByAddr(string multisig);
        Task<IEnumerable<IWalletAddress>> GetAllMultisigs();
        Task<ISegwitPrivateWallet> GetOrCreateSegwitPrivateWallet(string clientPubKey);
        Task<ISegwitPrivateWallet> CreateSegwitWallet();
        Task<ISegwitPrivateWallet> CreateSegwitWallet(PubKey pubKey, string clientPubKey = null);
    }

    public class WalletService : IWalletService
    {
        private readonly IWalletAddressRepository _walletAddressRepository;
        private readonly ISegwitPrivateWalletRepository _segwitPrivateWalletRepository;
        private readonly ISignatureApiProvider _signatureApiProvider;
        private readonly RpcConnectionParams _connectionParams;

        public WalletService(IWalletAddressRepository walletAddressRepository,
            ISignatureApiProvider signatureApiProvider,
            ISegwitPrivateWalletRepository segwitPrivateWalletRepository,
            RpcConnectionParams connectionParams)
        {
            _walletAddressRepository = walletAddressRepository;
            _segwitPrivateWalletRepository = segwitPrivateWalletRepository;
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

        public async Task<IWalletAddress> GetMultisigByAddr(string multisig)
        {
            return await _walletAddressRepository.GetByMultisig(multisig);
        }

        public async Task<IEnumerable<IWalletAddress>> GetAllMultisigs()
        {
            return await _walletAddressRepository.GetAllAddresses();
        }

        public async Task<ISegwitPrivateWallet> GetOrCreateSegwitPrivateWallet(string clientPubKey)
        {
            var segwitWallet = await _segwitPrivateWalletRepository.GetByClientPubKey(clientPubKey);
            if (segwitWallet != null)
                return segwitWallet;

            return await CreateSegwitWallet(new PubKey(await _signatureApiProvider.GeneratePubKey()), clientPubKey);
        }

        public async Task<ISegwitPrivateWallet> CreateSegwitWallet()
        {
            var segwitPubKey = new PubKey(await _signatureApiProvider.GeneratePubKey());
            return await CreateSegwitWallet(segwitPubKey);
        }

        public async Task<ISegwitPrivateWallet> CreateSegwitWallet(PubKey pubKey, string clientPubKey = null)
        {
            var segwitAddress = pubKey.WitHash.ScriptPubKey.Hash.GetAddress(_connectionParams.Network);

            return await _segwitPrivateWalletRepository.AddSegwitPrivateWallet(clientPubKey, segwitAddress.ToString(),
                pubKey.ToString(), pubKey.WitHash.ScriptPubKey.ToString());
        }

        private async Task<IWalletAddress> CreateMultisig(string clientPubKey, string exchangePubKey)
        {
            var redeemScript = MultisigHelper.GenerateMultisigRedeemScript(clientPubKey, exchangePubKey);

            var address = await _walletAddressRepository.Create(redeemScript.GetScriptAddress(_connectionParams.Network).ToString(), clientPubKey, exchangePubKey, redeemScript.ToString());
            
            return address;
        }
    }
}

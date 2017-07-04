using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Bitcoin;
using Core.Helpers;
using Core.Providers;
using Core.RabbitNotification;
using Core.Repositories.Wallets;
using LkeServices.Providers;

namespace LkeServices.Multisig
{
    public interface IMultisigService
    {
        Task<IWalletAddress> GetOrCreateMultisig(string clientPubKey);
        Task<IWalletAddress> GetMultisig(string clientPubKey);
        Task<IEnumerable<IWalletAddress>> GetAllMultisigs();
    }

    public class MultisigService : IMultisigService
    {
        private readonly IWalletAddressRepository _walletAddressRepository;
        private readonly IRabbitNotificationService _notificationService;
        private readonly ISignatureApiProvider _signatureApiProvider;
        private readonly RpcConnectionParams _connectionParams;

        public MultisigService(IWalletAddressRepository walletAddressRepository,
            Func<SignatureApiProviderType, ISignatureApiProvider> signatureApiProviderFactory,
            IRabbitNotificationService notificationService,
            RpcConnectionParams connectionParams)
        {
            _walletAddressRepository = walletAddressRepository;
            _notificationService = notificationService;
            _signatureApiProvider = signatureApiProviderFactory(SignatureApiProviderType.Exchange);
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

        public async Task<IEnumerable<IWalletAddress>> GetAllMultisigs()
        {
            return await _walletAddressRepository.GetAllAddresses();
        }

        private async Task<IWalletAddress> CreateMultisig(string clientPubKey, string exchangePubKey)
        {
            var scriptPubKey = MultisigHelper.GenerateMultisigRedeemScript(clientPubKey, exchangePubKey);
            var address = await _walletAddressRepository.Create(scriptPubKey.GetScriptAddress(_connectionParams.Network).ToWif(),
                clientPubKey, exchangePubKey, scriptPubKey.ToString());
            _notificationService.CreateMultisig(address.MultisigAddress, DateTime.UtcNow);
            return address;
        }
    }
}

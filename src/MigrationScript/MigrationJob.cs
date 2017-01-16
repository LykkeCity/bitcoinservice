using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Core.Repositories.Wallets;
using Lykke.Signing.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MigrationScript.Models;
using NBitcoin;

namespace MigrationScript
{
    public class MigrationJob
    {
        private readonly BitcoinContext _context;
        private readonly IWalletAddressRepository _walletAddressRepository;
        private readonly IKeyRepository _keyRepository;
        private readonly byte[] _password;

        public MigrationJob(BitcoinContext context, IWalletAddressRepository walletAddressRepository, IKeyRepository keyRepository, IConfiguration configuration)
        {
            _context = context;
            _walletAddressRepository = walletAddressRepository;
            _keyRepository = keyRepository;
            var sha = SHA256.Create();

            _password = sha.ComputeHash(Encoding.UTF8.GetBytes(configuration.GetValue<string>("BitcoinConfig:EncryptionPassword")));
        }

        public async Task Start()
        {
            var data = _context.SegKeys.ToList();
            Console.WriteLine($"Items count: [{data.Count}]");
            var idx = 0;
            foreach (var item in data)
            {
                try
                {
                    var secret = new BitcoinSecret(item.ExchangePrivateKey);
                    var pubKey = secret.PubKey.ToString();
                    var address = secret.GetAddress().ToWif();

                    var redeemScript =
                        PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, new PubKey(item.ClientPubKey),
                            secret.PubKey);

                    var multisigAddress = item.MultiSigAddress ?? redeemScript.GetScriptAddress(secret.Network).ToWif();

                    if (await _keyRepository.GetPrivateKey(address) == null)
                    {
                        var encrypted = Encryption.EncryptAesString(secret.ToWif(), _password);
                        await _keyRepository.CreatePrivateKey(address, encrypted);
                    }

                    if (await _walletAddressRepository.GetByClientPubKey(item.ClientPubKey) == null)
                        await _walletAddressRepository.Create(multisigAddress, item.ClientPubKey, pubKey, redeemScript.ToString());

                    idx++;
                    if (idx % 10 == 0)
                        Console.WriteLine($"Processed {idx} of {data.Count} records");
                }
                catch (Exception e)
                {
                    var c = Console.BackgroundColor;
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: db id [{item.Id}], exception: [{e.Message}]");
                    Console.BackgroundColor = c;
                }
            }
        }
    }
}

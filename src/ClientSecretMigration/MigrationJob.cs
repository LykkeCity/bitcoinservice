using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Lykke.Signing.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NBitcoin;

namespace ClientSecretMigration
{
    public class MigrationJob
    {
        private readonly IWalletCredentialsRepository _walletCredentials;
        private readonly byte[] _password;
        private readonly IKeyRepository _keyRepository;

        public MigrationJob(IKeyRepository keyRepository, IWalletCredentialsRepository walletCredentials, IConfiguration configuration)
        {
            _keyRepository = keyRepository;
            _walletCredentials = walletCredentials;

            var sha = SHA256.Create();

            _password = sha.ComputeHash(Encoding.UTF8.GetBytes(configuration.GetValue<string>("BitcoinConfig:EncryptionPassword")));
        }

        public async Task Start()
        {
            var data = (await _walletCredentials.GetDataAsync()).ToList();

            Console.WriteLine($"Items count: [{data.Count}]");
            var idx = 0;
            foreach (var item in data)
            {
                try
                {
                    var secret = new BitcoinSecret(item.PrivateKey);
                    var address = secret.GetAddress().ToString();

                    if (await _keyRepository.GetPrivateKey(address) == null)
                    {

                        var encrypted = Encryption.EncryptAesString(secret.ToString(), _password);
                        await _keyRepository.CreatePrivateKey(address, encrypted);
                    }

                    idx++;
                    if (idx % 10 == 0)
                        Console.WriteLine($"Processed {idx} of {data.Count} records");
                }
                catch (Exception e)
                {
                    var c = Console.BackgroundColor;
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: db id [{item.RowKey}], exception: [{e.Message}]");
                    Console.BackgroundColor = c;
                }
            }
        }
    }
}

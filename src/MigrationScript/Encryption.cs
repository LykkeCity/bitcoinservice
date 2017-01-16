using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Lykke.Signing.Utils
{
    public class Encryption
    {
        /// <summary>
        /// Decrypts base64 string with AES256 (key 32 bytes)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns>Decrypted string</returns>
        public static string DecryptAesString(string data, byte[] key)
        {
            string plaintext;

            using (var aesAlg = Aes.Create())
            {
                aesAlg.KeySize = 256;

                aesAlg.Key = key;
                aesAlg.IV = key.Take(16).ToArray();


                var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (var msDecrypt = new MemoryStream(Convert.FromBase64String(data)))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }

        /// <summary>
        /// Encrypts base64 data with AES256 (key 32 bytes)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="key"></param>
        /// <returns>encrypted string</returns>
        public static string EncryptAesString(string data, byte[] key)
        {
            byte[] encrypted;

            using (var aesAlg = Aes.Create())
            {
                aesAlg.KeySize = 256;

                aesAlg.Key = key;
                aesAlg.IV = key.Take(16).ToArray();

                var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(data);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(encrypted);
        }
    }
}

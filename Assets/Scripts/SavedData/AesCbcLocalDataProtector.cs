using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace SavedData
{
    public class AesCbcLocalDataProtector : ILocalDataProtector
    {
        private const string Format = "SF_AES";
        private const int Version = 1;
        private const int IvSizeBytes = 16;

        private readonly byte[] _encryptionKey;

        public AesCbcLocalDataProtector(ILocalSecretProvider secretProvider)
        {
            _encryptionKey = DeriveKey(secretProvider.GetOrCreateSecret());
        }

        public string Protect(string plainText)
        {
            var iv = RandomBytes(IvSizeBytes);
            var cipherTextBytes = EncryptAesCbc(Encoding.UTF8.GetBytes(plainText), _encryptionKey, iv);

            var payload = new ProtectedPayload
            {
                Format = Format,
                Version = Version,
                Iv = Convert.ToBase64String(iv),
                CipherText = Convert.ToBase64String(cipherTextBytes)
            };

            return JsonConvert.SerializeObject(payload);
        }

        public bool TryUnprotect(string protectedText, out string plainText)
        {
            plainText = null;

            ProtectedPayload payload;
            try
            {
                payload = JsonConvert.DeserializeObject<ProtectedPayload>(protectedText);
            }
            catch
            {
                return false;
            }

            if (payload == null || payload.Format != Format || payload.Version != Version)
            {
                return false;
            }

            byte[] iv;
            byte[] cipherText;
            try
            {
                iv = Convert.FromBase64String(payload.Iv);
                cipherText = Convert.FromBase64String(payload.CipherText);
            }
            catch
            {
                return false;
            }

            byte[] plainBytes;
            try
            {
                plainBytes = DecryptAesCbc(cipherText, _encryptionKey, iv);
            }
            catch
            {
                return false;
            }

            plainText = Encoding.UTF8.GetString(plainBytes);
            return true;
        }

        private static byte[] DeriveKey(string secret)
        {
            byte[] secretBytes;
            try
            {
                secretBytes = Convert.FromBase64String(secret);
            }
            catch
            {
                secretBytes = Encoding.UTF8.GetBytes(secret);
            }

            using var sha = SHA256.Create();
            return sha.ComputeHash(secretBytes);
        }

        private static byte[] EncryptAesCbc(byte[] plainBytes, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }

        private static byte[] DecryptAesCbc(byte[] cipherBytes, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        }

        private static byte[] RandomBytes(int length)
        {
            var bytes = new byte[length];
            using var random = RandomNumberGenerator.Create();
            random.GetBytes(bytes);
            return bytes;
        }

        [Serializable]
        private class ProtectedPayload
        {
            public string Format;
            public int Version;
            public string Iv;
            public string CipherText;
        }
    }
}

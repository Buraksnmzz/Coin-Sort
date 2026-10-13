using System;
using System.IO;
using UnityEngine;

namespace SavedData
{
    public class PersistentFileLocalSecretProvider : ILocalSecretProvider
    {
        private const string SecretFileName = "SF_LOCAL_SAVE_SECRET_V1.key";

        public string GetOrCreateSecret()
        {
            var fullPath = Path.Combine(Application.persistentDataPath, SecretFileName);

            try
            {
                if (File.Exists(fullPath))
                {
                    var existing = File.ReadAllText(fullPath);
                    if (!string.IsNullOrWhiteSpace(existing))
                    {
                        return existing;
                    }
                }

                var secretBytes = new byte[32];
                using (var random = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    random.GetBytes(secretBytes);
                }

                var secret = Convert.ToBase64String(secretBytes);
                File.WriteAllText(fullPath, secret);
                return secret;
            }
            catch
            {
                var fallbackBytes = new byte[32];
                using (var random = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    random.GetBytes(fallbackBytes);
                }

                return Convert.ToBase64String(fallbackBytes);
            }
        }
    }
}
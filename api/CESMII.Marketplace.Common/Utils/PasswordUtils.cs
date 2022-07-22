namespace CESMII.Marketplace.Common
{
    using System;
    using System.Text;
    using System.Security.Cryptography;
    using Microsoft.AspNetCore.Cryptography.KeyDerivation;

    using CESMII.Marketplace.Common.Models;

    public static class PasswordUtils
    {
        private static readonly string _delimiter = "$";

        /// <summary>
        /// Generate a random password with no special characters
        /// </summary>
        /// <param name="length">
        /// The desired length of the password to be generated.
        /// </param>
        /// <returns>
        /// Randomly generated password.
        /// </returns>
        public static string GenerateRandomPassword(int length = 8)
        {
            var rng = RandomNumberGenerator.Create();
            var bits = (length * 6);
            var byte_size = ((bits + 7) / 8);
            var bytesarray = new byte[byte_size];
            rng.GetBytes(bytesarray);
            return Convert.ToBase64String(bytesarray);
        }

        /// <summary>
        /// Encrypt a new password.This will also include key derivation value, num bytes, iterations, salt, 
        /// </summary>
        /// <returns>
        /// Encrypted value of the password provided.
        /// </returns>
        public static string EncryptNewPassword(EncryptionConfig encrConfig, string password)
        {
            //populate new salt w/ random bytes
            var rng = RandomNumberGenerator.Create();
            var randomSalt = new byte[16];
            rng.GetBytes(randomSalt);

            EncryptionLevelConfig encrLevel = encrConfig.Levels.Find(e => e.Id == encrConfig.CurrentLevel);
            if (encrLevel == null) throw new ArgumentNullException($"Encryption Config Level is missing. Check appSettings.json. Current Level: {encrConfig.CurrentLevel}");

            // derive a 256-bit sub key (use HMACSHA256 with 20,000 iterations)
            var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: randomSalt,
                prf: encrLevel.PRF,
                iterationCount: encrLevel.Iterations,
                numBytesRequested: encrLevel.NumBytes));

            //return 

            return $"{encrLevel.Id}{_delimiter}" +
                    $"{Convert.ToBase64String(randomSalt)}{_delimiter}" +
                    $"{hashed}";
        }

        /// <summary>
        /// Validate an existing password, Pass in the user's existing encoded password (a concatenated string separated by $
        /// with <encryption config id>$<salt>$<hash>). Encrypt the incoming password using the same
        /// settings in the encoded value and see if it matches on the hash
        /// </summary>
        /// <param name="encoded"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool ValidatePassword(EncryptionConfig encrConfig, string encoded, string password, out bool updatePasswordEncryption)
        {
            updatePasswordEncryption = false;
            if (string.IsNullOrEmpty(encoded)) return false;
            if (string.IsNullOrEmpty(password)) return false;

            string[] passwordParts = encoded.Split(_delimiter);
            if (passwordParts.Length != 3) return false;

            //parse parts into their positions. Then take the plain password and excrypt to see if it matches the hash
            int encrId = Convert.ToInt32(passwordParts[0]);
            EncryptionLevelConfig encrLevel = encrConfig.Levels.Find(e => e.Id == encrId);
            if (encrLevel == null) throw new ArgumentNullException($"Encryption Config is missing. Check appSettings.json. Current Level: {encrConfig.CurrentLevel}");

            KeyDerivationPrf prf = (KeyDerivationPrf)Convert.ToInt16(encrLevel.PRF);
            int numBytes = Convert.ToInt32(encrLevel.NumBytes);
            int iterations = Convert.ToInt32(encrLevel.Iterations);
            var convertedSalt = Convert.FromBase64String(passwordParts[1]);

            //perform hash
            var hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: convertedSalt,
                prf: prf,
                iterationCount: iterations,
                numBytesRequested: numBytes);
            var hashed = Convert.ToBase64String(hash);

            //add support to update pw to latest level if current pw was a lower level.
            //have to get UserDAL the new password and it has to save it.
            updatePasswordEncryption = encrConfig.CurrentLevel > encrId;

            //true if they match, existing hash in 3rd position
            return hashed == passwordParts[2];
        }

        #region Encrypting / Decrypting Paired Methods
        /// <summary>
        /// This will generate a random key, encrypt the value and return 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string EncryptString(string text, string key)
        {
            //split key into 2 parts using a pre-defined delimeter
            string[] keyParts = key.Split(_delimiter);
            if (keyParts.Length != 2) throw new ArgumentException("Invalid key value");

            Aes cipher = CreateCipher(keyParts[0]);
            cipher.IV = Convert.FromBase64String(keyParts[1]);

            ICryptoTransform cryptTransform = cipher.CreateEncryptor();
            byte[] plaintext = Encoding.UTF8.GetBytes(text);
            byte[] cipherText = cryptTransform.TransformFinalBlock(plaintext, 0, plaintext.Length);

            return Convert.ToBase64String(cipherText);
        }

        public static string DecryptString(string encryptedText, string key)
        {
            string[] keyParts = key.Split(_delimiter);
            if (keyParts.Length != 2) throw new ArgumentException("Invalid key value");

            Aes cipher = CreateCipher(keyParts[0]);
            cipher.IV = Convert.FromBase64String(keyParts[1]);

            ICryptoTransform cryptTransform = cipher.CreateDecryptor();
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] plainBytes = cryptTransform.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }

        //public static (string key, string ivBase64) InitSymmetricEncryptionKeyIV()
        //{
        //    var byteArray = new byte[32]; //256
        //    RandomNumberGenerator.Fill(byteArray);
        //    var key = Convert.ToBase64String(byteArray); 
        //    Aes cipher = CreateCipher(key);
        //    var ivBase64 = Convert.ToBase64String(cipher.IV);
        //    return (key, ivBase64);
        //}

        private static Aes CreateCipher(string keyBase64)
        {
            // Default values: Keysize 256, Padding PKC27
            Aes cipher = Aes.Create();
            cipher.Mode = CipherMode.CBC;  // Ensure the integrity of the ciphertext if using CBC

            cipher.Padding = PaddingMode.ISO10126;
            cipher.Key = Convert.FromBase64String(keyBase64);

            return cipher;
        }
        #endregion
    }
}

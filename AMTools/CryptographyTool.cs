using System.Security.Cryptography;
using System.Text;

namespace AMTools.Tools
{
    public static class CryptographyTool
    {
        private static readonly string Key = "MySuperSecureKey1234567890123456"; // Must be 16, 24, or 32 bytes
        private static readonly string IV = "MySecureIV123456"; // Must be 16 bytes

        public static void Encrypt(string plainText, out string encryptedText)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(Key);
            aes.IV = Encoding.UTF8.GetBytes(IV);

            using MemoryStream ms = new();
            using CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using StreamWriter writer = new(cs);
            writer.Write(plainText);
            writer.Flush(); // Ensures all data is written
            cs.FlushFinalBlock(); // Ensures all encryption data is finalized

            encryptedText = Convert.ToBase64String(ms.ToArray());
        }

        public static void Decrypt(string encryptedText, out string plainText)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(Key);
            aes.IV = Encoding.UTF8.GetBytes(IV);

            byte[] cipherBytes = Convert.FromBase64String(encryptedText);

            using MemoryStream ms = new(cipherBytes);
            using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader reader = new(cs);

            plainText = reader.ReadToEnd();
        }
    }
}

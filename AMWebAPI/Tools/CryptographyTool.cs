using System.Security.Cryptography;
using System.Text;

namespace AMWebAPI.Tools
{
    public static class CryptographyTool
    {
        private static readonly string Key = "MySuperSecureKey1234567890123456"; // Must be 16, 24, or 32 bytes
        private static readonly string IV = "MySecureIV123456"; // Must be 16 bytes

        public static string Encrypt(string plainText)
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

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(Key);
            aes.IV = Encoding.UTF8.GetBytes(IV);

            using MemoryStream ms = new(Convert.FromBase64String(cipherText));
            using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader reader = new(cs);

            return reader.ReadToEnd();
        }
    }
}

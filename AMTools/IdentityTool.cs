using System.Security.Cryptography;
using System.Text;

namespace AMTools
{
    public static class IdentityTool
    {
        public static string GenerateSaltString()
        {
            var salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        public static string GenerateRandomPassword()
        {
            var length = 12;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-_=+";
            StringBuilder result = new(length);
            byte[] data = new byte[length];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data); // Fill byte array with cryptographically strong random bytes
            }

            for (int i = 0; i < length; i++)
            {
                result.Append(chars[data[i] % chars.Length]); // Map byte to a valid character
            }

            return result.ToString();
        }

        public static string HashPassword(string password, string salt)
        {
            var saltByte = Convert.FromBase64String(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltByte, 100000, HashAlgorithmName.SHA256))
            {
                return Convert.ToBase64String(pbkdf2.GetBytes(32));
            }
        }

        public static string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)); // 64 bytes for security
        }
    }
}

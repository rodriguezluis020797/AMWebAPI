using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

        public static ClaimsPrincipal GetClaimsFromJwt(string token, string secretKey)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // You can add validation parameters (optional)
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false, // if you don't need to validate issuer
                ValidateAudience = false, // if you don't need to validate audience
                ValidateLifetime = true, // Ensure token has not expired
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), // Validate signing key
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                // Decode and validate the token
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return principal; // Returns the ClaimsPrincipal with the decoded claims
            }
            catch (Exception ex)
            {
                // Handle invalid or expired token
                throw new UnauthorizedAccessException("Invalid or expired token", ex);
            }
        }

        public static string GenerateJWTToken(Claim[] claims, string keyString, string issuer, string audience, string expiresInMinutes)
        {
            var key = Encoding.UTF8.GetBytes(keyString);
            var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(expiresInMinutes));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}

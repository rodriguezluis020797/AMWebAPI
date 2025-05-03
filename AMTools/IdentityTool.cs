using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace AMTools;

public static class IdentityTool
{
    
    public static long GetJwtClaimById(string jwToken, string key, string claimValue)
    {
        var claims = IdentityTool.GetClaimsFromJwt(jwToken, key);
        if (!long.TryParse(claims.FindFirst(claimValue)?.Value, out var providerId))
            throw new ArgumentException("Invalid provider ID in JWT.");

        return providerId;
    }
    
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
        var data = new byte[length];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(data); // Fill byte array with cryptographically strong random bytes
        }

        for (var i = 0; i < length; i++) result.Append(chars[data[i] % chars.Length]); // Map byte to a valid character

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

    public static string GenerateJWTToken(Claim[] claims, string keyString, string issuer, string audience,
        string expiresInMinutes)
    {
        var key = Encoding.UTF8.GetBytes(keyString);
        var expires =
            DateTime.UtcNow.AddSeconds(Convert
                .ToDouble(10)); //DateTime.UtcNow.AddMinutes(Convert.ToDouble(expiresInMinutes));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public static ClaimsPrincipal GetClaimsFromJwt(string token, string secretKey)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException("Invalid or expired token", ex);
        }
    }

    public static bool IsTheJWTExpired(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);
        var exp = jsonToken.ValidTo;

        return exp < DateTime.UtcNow;
    }

    public static bool IsValidPassword(string password)
    {
        return Regex.IsMatch(password,
            @"^(?!.*[\'\""\\<>|; \t:/$^~`()!?]).*(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[#%&_+]).{8,}$");
    }
}
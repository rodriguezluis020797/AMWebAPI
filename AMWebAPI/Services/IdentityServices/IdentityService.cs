﻿using AMWebAPI.Models.CoreModels;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Models.IdentityModels;
using AMWebAPI.Services.DataServices;
using AMWebAPI.Tools;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;

namespace AMWebAPI.Services.IdentityServices
{
    public interface IIdentityService
    {
        public UserDTO LogIn(UserDTO dto, string ipAddress);
        public void CreateNewPassword(UserDTO dto, bool tempPassword);
    }
    public class IdentityService : IIdentityService
    {
        private readonly IAMLogger _logger;
        private readonly AMCoreData _coreData;
        private readonly AMIdentityData _identityData;
        private readonly IConfiguration _configuration;
        public IdentityService(AMCoreData coreData, AMIdentityData identityData, IAMLogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _coreData = coreData;
            _configuration = configuration;
            _identityData = identityData;
        }
        public UserDTO LogIn(UserDTO dto, string ipAddress)
        {
            var user = _coreData.Users
                .Where(x => x.EMail.Equals(dto.EMail))
                .FirstOrDefault();

            if (user == null)
            {
                dto.Password = string.Empty;
                dto.RequestStatus = Models.RequestStatusEnum.BadRequest;
                return dto;
            }

            var decryptedPassword = string.Empty; //get after setting up identity server, check if it is a temp passowrd

            if (!dto.Password.Equals(decryptedPassword))
            {
                dto.Password = string.Empty;
                dto.RequestStatus = Models.RequestStatusEnum.BadRequest;
                return dto;
            }
            var session = new SessionModel()
            {
                CreateDate = DateTime.UtcNow,
                SessionId = 0,
                UserId = user.UserId
            };
            var refreshToken = GenerateRefreshToken(user.UserId);

            dto.CreateNewRecordFromModel(user);

            dto.RefreshToken = EncryptionTool.Encrypt(refreshToken.Token);
            dto.JWTToken = GenerateJWTToken(dto.UserId, dto.EMail, session.SessionId.ToString());


            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                _coreData.Sessions.Add(session);
                _coreData.SaveChanges();

                _identityData.RefreshTokens.Add(refreshToken);
                _identityData.SaveChanges();

                scope.Complete();
            }

            dto.RequestStatus = Models.RequestStatusEnum.Success;
            _logger.LogAudit($"User Id: {user.UserId}{Environment.NewLine}" +
                $"IP Address: {ipAddress}");

            return dto;
        }
        public void CreateNewPassword(UserDTO dto, bool tempPassword)
        {
            var salt = GenerateSalt();
            var hash = HashPassword(dto.Password, salt);

            var saltString = Convert.ToBase64String(salt);
            var hashString = Convert.ToBase64String(hash);

            var passwordModel = new PasswordModel()
            {
                CreateDate = DateTime.Now,
                DeleteDate = null,
                HashedPassword = hashString,
                PasswordId = 0,
                Salt = saltString,
                UserId = Convert.ToInt64(EncryptionTool.Decrypt(dto.UserId)),
                Temporary = tempPassword,
            };

            _identityData.Passwords.Add(passwordModel);
            _identityData.SaveChanges();
        }
        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
        public byte[] HashPassword(string password, byte[] salt, int iterations = 100000, int hashSize = 32)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(hashSize);
            }
        }
        public string GenerateJWTToken(string userId, string email, string sessionId)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiresInMinutes"]));

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Sid, sessionId),
            };

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
        public RefreshTokenModel GenerateRefreshToken(long userId)
        {
            var refreshToken = new RefreshTokenModel
            {
                Token = Guid.NewGuid().ToString(),
                ExpiresDate = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]!)),
                CreateDate = DateTime.UtcNow,
                UserId = userId
            };

            return refreshToken;
        }
        public bool ValidateRefreshToken(long userId, string token)
        {
            var refreshToken = _identityData.RefreshTokens
                .Where(x => x.UserId == userId && DateTime.UtcNow < x.ExpiresDate)
                .FirstOrDefault();
            if (refreshToken == null)
            {
                return false;
            }
            return refreshToken.Token.Equals(token);
        }
        public bool VerifyPassword(string enteredPassword, long userId)
        {
            var passwordModel = _identityData.Passwords
                .Where(x => x.UserId == userId && x.DeleteDate == null)
                .FirstOrDefault();

            if(passwordModel == null)
            {
                return false;
            }

            var salt = Convert.FromBase64String(passwordModel.Salt);
            var hash = Convert.FromBase64String(passwordModel.HashedPassword);

            byte[] computedHash = HashPassword(enteredPassword, salt);
            return CryptographicOperations.FixedTimeEquals(computedHash, hash);
        }
    }
}
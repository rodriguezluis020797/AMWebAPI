using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.IdentityModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.DataServices;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Transactions;

namespace AMWebAPI.Services.IdentityServices
{
    public interface IIdentityService
    {
        public UserDTO LogIn(UserDTO dto, string ipAddress);
        public bool CreateNewPassword(long userId, string password, bool isTempPassword);
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
                dto.RequestStatus = RequestStatusEnum.BadRequest;
                return dto;
            }

            var passwordModels = _identityData.Passwords
                .Where(x => x.UserId == user.UserId && x.DeleteDate == null)
                .ToList();

            //Should never be hit, but never know
            if (passwordModels.Count > 1 || !passwordModels.Any())
            {
                throw new Exception(nameof(passwordModels.Count));
            }

            var passwordModel = passwordModels.Single();

            var hashedPassword = IdentityTool.HashPassword(dto.Password, passwordModel.Salt);

            if (!hashedPassword.Equals(passwordModel.HashedPassword))
            {
                dto.Password = string.Empty;
                dto.RequestStatus = RequestStatusEnum.BadRequest;
                return dto;
            }
            else
            {
                /*
                 * TODO:
                 * - Session
                 * - JWT
                 * - Refresh Token
                 */

                if (passwordModel.Temporary)
                {
                    dto.IsTempPassword = true;
                }
                else
                {
                    dto.IsTempPassword = false;
                }

                var session = new SessionModel()
                {
                    CreateDate = DateTime.UtcNow,
                    SessionId = 0,
                    UserId = user.UserId
                };
                var refreshToken = new RefreshTokenModel()
                {
                    CreateDate = DateTime.UtcNow,
                    ExpiresDate = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]!)),
                    RefreshTokenId = 0,
                    Token = IdentityTool.GenerateRefreshToken(),
                    UserId = user.UserId
                };

                dto.CreateNewRecordFromModel(user);

                CryptographyTool.Encrypt(refreshToken.Token, out string encryptedToken);
                dto.RefreshToken = encryptedToken;
                dto.JWTToken = GenerateJWTToken(dto.UserId, dto.EMail, session.SessionId.ToString());


                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    _coreData.Sessions.Add(session);
                    _coreData.SaveChanges();

                    _identityData.RefreshTokens.Add(refreshToken);
                    _identityData.SaveChanges();

                    scope.Complete();
                }

                dto.RequestStatus = RequestStatusEnum.Success;
                _logger.LogAudit($"User Id: {user.UserId}{Environment.NewLine}" +
                    $"IP Address: {ipAddress}");
            }

            return dto;
        }
        public bool CreateNewPassword(long userId, string password, bool isTempPassword)
        {
            var tempPassword = string.Empty;
            var hash = string.Empty;
            var salt = IdentityTool.GenerateSaltString();

            if (isTempPassword)
            {
                tempPassword = Guid.NewGuid().ToString().Replace("-", "");
                hash = IdentityTool.HashPassword(password, salt);
            }
            else
            {
                hash = IdentityTool.HashPassword(password, salt);
            }
            /*
            var saltString = Convert.ToBase64String(salt);
            var hashString = Convert.ToBase64String(hash);

            var passwordModel = new PasswordModel()
            {
                CreateDate = DateTime.Now,
                DeleteDate = null,
                HashedPassword = hashString,
                PasswordId = 0,
                Salt = saltString,
                UserId = userId,
                Temporary = isTempPassword,
            };

            var currentPasswords = _identityData.Passwords
                .Where(x => x.UserId == userId && x.DeleteDate == null)
                .ToList();

            using (var trans = _identityData.Database.BeginTransaction())
            {
                foreach (var cp in currentPasswords)
                {
                    cp.DeleteDate = DateTime.UtcNow;
                    _identityData.Passwords.Update(cp);
                    _identityData.SaveChanges();
                }

                _identityData.Passwords.Add(passwordModel);
                _identityData.SaveChanges();

                trans.Commit();
            }
            */
            return true;
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
    }
}
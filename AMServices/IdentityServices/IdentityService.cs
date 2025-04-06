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
        public UserDTO LogIn(UserDTO dto, string ipAddress, out string jwToken, out string refreshToken);
        public UserDTO UpdatePassword(UserDTO dto, string token);
        public string RefreshJWToken(string jwtToken, string refreshToken, string ipAddress);
        public ClaimsPrincipal GetClaimsFromJwt(string token, string secretKey);
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
        public UserDTO LogIn(UserDTO dto, string ipAddress, out string jwToken, out string refreshToken)
        {
            var user = _coreData.Users
                .Where(x => x.EMail.Equals(dto.EMail))
                .FirstOrDefault();

            if (user == null)
            {
                throw new ArgumentException();
            }

            var passwordModels = _identityData.Passwords
                .Where(x => x.UserId == user.UserId && x.DeleteDate == null)
                .ToList();

            //Should never be hit, but never know
            if (passwordModels.Count > 1 || !passwordModels.Any())
            {
                //set temp password, email user
                throw new Exception(nameof(passwordModels.Count));
            }

            var passwordModel = passwordModels.Single();

            if (passwordModel == null)
            {
                throw new Exception(nameof(passwordModel));
            }

            var hashedPassword = IdentityTool.HashPassword(dto.Password, passwordModel.Salt);

            if (!hashedPassword.Equals(passwordModel.HashedPassword))
            {
                throw new ArgumentException();
            }
            else
            {
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
                var sessionAction = new SessionActionModel()
                {
                    CreateDate = DateTime.UtcNow,
                    SessionId = session.SessionId,
                    SessionAction = SessionActionEnum.LogIn,
                    SessionActionId = 0
                };
                var refreshTokenModel = new RefreshTokenModel()
                {
                    CreateDate = DateTime.UtcNow,
                    ExpiresDate = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]!)),
                    RefreshTokenId = 0,
                    Token = IdentityTool.GenerateRefreshToken(),
                    UserId = user.UserId,
                    DeleteDate = null,
                    FingerPrint = ipAddress
                };

                CryptographyTool.Encrypt(refreshTokenModel.Token, out string encryptedRefreshToken);

                refreshToken = encryptedRefreshToken;

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    _coreData.Sessions.Add(session);
                    _coreData.SaveChanges();

                    sessionAction.SessionId = session.SessionId;
                    _coreData.SessionActions.Add(sessionAction);
                    _coreData.SaveChanges();

                    var existingRefreshTokens = _identityData.RefreshTokens
                        .Where(x => x.UserId == user.UserId && x.DeleteDate == null && x.FingerPrint.Equals(ipAddress))
                        .ToList();
                    foreach (var existingRefreshToken in existingRefreshTokens)
                    {
                        existingRefreshToken.DeleteDate = DateTime.UtcNow;
                        _identityData.RefreshTokens.Update(existingRefreshToken);
                        _identityData.SaveChanges();
                    }

                    _identityData.RefreshTokens.Add(refreshTokenModel);
                    _identityData.SaveChanges();

                    scope.Complete();
                }

                var claims = new[]
                {
                    new Claim(SessionClaimEnum.UserId.ToString(), user.UserId.ToString()),
                    new Claim(SessionClaimEnum.SessionId.ToString(), session.SessionId.ToString()),
                };

                jwToken = IdentityTool.GenerateJWTToken(claims, _configuration["Jwt:Key"]!, _configuration["Jwt:Issuer"]!, _configuration["Jwt:Audience"]!, _configuration["Jwt:ExpiresInMinutes"]!);

                _logger.LogAudit($"User Id: {user.UserId}{Environment.NewLine}" +
                    $"IP Address: {ipAddress}");

                dto.CreateNewRecordFromModel(user);
                dto.IsTempPassword = passwordModel.Temporary;
            }

            return dto;
        }

        public UserDTO UpdatePassword(UserDTO dto, string token)
        {
            var principal = GetClaimsFromJwt(token, _configuration["Jwt:Key"]!);

            var userId = principal.FindFirst(SessionClaimEnum.UserId.ToString())?.Value;
            var sessionId = principal.FindFirst(SessionClaimEnum.SessionId.ToString())?.Value;



            throw new NotImplementedException();
        }

        public string RefreshJWToken(string jwtToken, string refreshToken, string ipAddress)
        {
            var principal = GetClaimsFromJwt(jwtToken, _configuration["Jwt:Key"]!);

            var userId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.UserId.ToString())?.Value);
            var sessionId = principal.FindFirst(SessionClaimEnum.SessionId.ToString())?.Value;

            var refreshTokenModel = _identityData.RefreshTokens
                .Where(x => x.UserId == userId && DateTime.UtcNow < x.ExpiresDate && x.FingerPrint.Equals(ipAddress) && x.DeleteDate == null)
                .FirstOrDefault();

            if (refreshTokenModel == null)
            {
                throw new ArgumentException();
            }

            CryptographyTool.Decrypt(refreshToken, out string decryptedToken);

            if (!refreshTokenModel.Token.Equals(decryptedToken) || !refreshTokenModel.FingerPrint.Equals(ipAddress))
            {
                throw new ArgumentException();
            }

            refreshTokenModel.ExpiresDate = refreshTokenModel.ExpiresDate.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]!));
            _identityData.RefreshTokens.Update(refreshTokenModel);
            _identityData.SaveChanges();

            var claims = new[]
            {
                new Claim(SessionClaimEnum.UserId.ToString(), userId.ToString()),
                new Claim(SessionClaimEnum.SessionId.ToString(), sessionId),
            };

            return IdentityTool.GenerateJWTToken(claims, _configuration["Jwt:Key"]!, _configuration["Jwt:Issuer"]!, _configuration["Jwt:Audience"]!, "-1");// _configuration["Jwt:ExpiresInMinutes"]!);

        }

        public ClaimsPrincipal GetClaimsFromJwt(string token, string secretKey)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // You can add validation parameters (optional)
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false, // if you don't need to validate issuer
                ValidateAudience = false, // if you don't need to validate audience
                ValidateLifetime = false, // Ensure token has not expired
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
    }
}
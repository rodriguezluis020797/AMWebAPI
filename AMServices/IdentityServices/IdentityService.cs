using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMData.Models.IdentityModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Transactions;
using static AMWebAPI.Services.IdentityServices.IdentityService;

namespace AMWebAPI.Services.IdentityServices
{
    public interface IIdentityService
    {
        public Task<LogInAsyncResponse> LogInAsync(UserDTO userDTO, FingerprintDTO fingerprintDTO);
        public Task<UserDTO> UpdatePasswordAsync(UserDTO dto, string token);
        public Task<string> RefreshJWToken(string jwtToken, string refreshToken, FingerprintDTO fingerprintDTO);
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
        public async Task<LogInAsyncResponse> LogInAsync(UserDTO userDTO, FingerprintDTO fingerprintDTO)
        {
            var response = new LogInAsyncResponse();

            var user = await _coreData.Users
                .Where(x => x.EMail.Equals(userDTO.EMail))
                .FirstOrDefaultAsync();

            if (user == null)
            {
                throw new ArgumentException();
            }

            var passwordModel = await _identityData.Passwords
                .Where(x => x.UserId == user.UserId)
                .OrderByDescending(x => x.CreateDate)
                .FirstOrDefaultAsync();


            if (passwordModel == null)
            {
                throw new Exception(nameof(passwordModel));
            }

            var hashedPassword = IdentityTool.HashPassword(userDTO.Password, passwordModel.Salt);

            if (!hashedPassword.Equals(passwordModel.HashedPassword))
            {
                throw new ArgumentException();
            }
            else
            {
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
                    IPAddress = fingerprintDTO.IPAddress,
                    Language = fingerprintDTO.Language,
                    Platform = fingerprintDTO.Platform,
                    UserAgent = fingerprintDTO.UserAgent
                };

                CryptographyTool.Encrypt(refreshTokenModel.Token, out string encryptedRefreshToken);

                response.refreshToken = encryptedRefreshToken;

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    await _coreData.Sessions.AddAsync(session);
                    await _coreData.SaveChangesAsync();

                    sessionAction.SessionId = session.SessionId;
                    await _coreData.SessionActions.AddAsync(sessionAction);
                    await _coreData.SaveChangesAsync();

                    var existingRefreshTokens = await _identityData.RefreshTokens
                        .Where(x => x.UserId == user.UserId && x.DeleteDate == null)
                        .ToListAsync();

                    foreach (var existingRefreshToken in existingRefreshTokens)
                    {
                        existingRefreshToken.DeleteDate = DateTime.UtcNow;
                        _identityData.RefreshTokens.Update(existingRefreshToken);
                        await _identityData.SaveChangesAsync();
                    }

                    await _identityData.RefreshTokens.AddAsync(refreshTokenModel);
                    await _identityData.SaveChangesAsync();

                    scope.Complete();
                }

                var claims = new[]
                {
                    new Claim(SessionClaimEnum.UserId.ToString(), user.UserId.ToString()),
                    new Claim(SessionClaimEnum.SessionId.ToString(), session.SessionId.ToString()),
                };

                response.jwToken = IdentityTool.GenerateJWTToken(claims, _configuration["Jwt:Key"]!, _configuration["Jwt:Issuer"]!, _configuration["Jwt:Audience"]!, _configuration["Jwt:ExpiresInMinutes"]!);

                _logger.LogAudit($"User Id: {user.UserId}{Environment.NewLine}" +
                  $"IP Address: {fingerprintDTO.IPAddress}" +
                  $"UserAgent: {fingerprintDTO.UserAgent}" +
                  $"Platform: {fingerprintDTO.Platform}" +
                  $"Language: {fingerprintDTO.Language}");

                userDTO.CreateNewRecordFromModel(user);
                userDTO.IsTempPassword = passwordModel.Temporary;

                response.userDTO = userDTO;
            }

            return response;
        }

        public async Task<UserDTO> UpdatePasswordAsync(UserDTO dto, string token)
        {
            if (!IdentityTool.IsValidPassword(dto.Password))
            {
                throw new ArgumentException();
            }

            var principal = GetClaimsFromJwt(token, _configuration["Jwt:Key"]!);

            var userId = principal.FindFirst(SessionClaimEnum.UserId.ToString())?.Value;
            var sessionId = principal.FindFirst(SessionClaimEnum.SessionId.ToString())?.Value;

            var userModel = await _coreData.Users
                .Where(x => x.UserId == Convert.ToInt64(userId))
                .FirstOrDefaultAsync();

            if (userModel == null)
            {
                throw new Exception(nameof(userId));
            }

            var passwordModelList = await _identityData.Passwords
                .Where(x => x.UserId == userModel.UserId)
                .OrderByDescending(x => x.CreateDate)
                .Take(5)
                .ToListAsync();

            if (!passwordModelList.Any())
            {
                throw new Exception(nameof(passwordModelList.Count));
            }

            foreach (var password in passwordModelList)
            {
                if (password.HashedPassword.Equals(IdentityTool.HashPassword(dto.Password, password.Salt)))
                {
                    throw new ArgumentException();
                }
            }

            var passwordSalt = IdentityTool.GenerateSaltString();
            var newPassword = new PasswordModel()
            {
                CreateDate = DateTime.UtcNow,
                DeleteDate = null,
                HashedPassword = IdentityTool.HashPassword(dto.Password, passwordSalt),
                PasswordId = 0,
                Salt = passwordSalt,
                Temporary = false,
                UserId = userModel.UserId
            };
            var userComm = new UserCommunicationModel()
            {
                AttemptOne = null,
                UserId = userModel.UserId,
                AttemptThree = null,
                AttemptTwo = null,
                CommunicationId = 0,
                CreateDate = DateTime.UtcNow,
                DeleteDate = null,
                Message = "Your password was recently changed. If you did not request this change please change your password immediately or contact customer service."
            };


            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await _coreData.AddAsync(userComm);
                await _coreData.SaveChangesAsync();

                await _identityData.Passwords.AddAsync(newPassword);
                await _identityData.SaveChangesAsync();

                scope.Complete();
            }

            dto.CreateNewRecordFromModel(userModel);
            return dto;
        }

        public async Task<string> RefreshJWToken(string jwtToken, string refreshToken, FingerprintDTO fingerprintDTO)
        {

            var principal = GetClaimsFromJwt(jwtToken, _configuration["Jwt:Key"]!);

            var userId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.UserId.ToString())?.Value);
            var sessionId = principal.FindFirst(SessionClaimEnum.SessionId.ToString())?.Value;

            var refreshTokenModel = await _identityData.RefreshTokens
                .Where(x => x.UserId == userId && DateTime.UtcNow < x.ExpiresDate && x.DeleteDate == null)
                .OrderByDescending(x => x.CreateDate)
                .FirstOrDefaultAsync();

            if (refreshTokenModel == null)
            {
                throw new ArgumentException();
            }

            var existingFingerprint = new FingerprintDTO
            {
                IPAddress = refreshTokenModel.IPAddress,
                Language = refreshTokenModel.Language,
                Platform = refreshTokenModel.Platform,
                UserAgent = refreshTokenModel.UserAgent
            };

            if (!IsFingerprintTrustworthy(existingFingerprint, fingerprintDTO))
            {
                throw new ArgumentException();
            }

            CryptographyTool.Decrypt(refreshToken, out string decryptedToken);

            if (!refreshTokenModel.Token.Equals(decryptedToken))//|| !refreshTokenModel.FingerPrint.Equals(ipAddress))
            {
                throw new ArgumentException();
            }

            refreshTokenModel.ExpiresDate = refreshTokenModel.ExpiresDate.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]!));
            _identityData.RefreshTokens.Update(refreshTokenModel);
            await _identityData.SaveChangesAsync();

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

        private bool IsFingerprintTrustworthy(FingerprintDTO databaseFingerprint, FingerprintDTO providedFingerpirnt)
        {
            var trustScore = 0;
            var maxScore = 100;
            var trustScoreThreshold = 80;

            if (databaseFingerprint.IPAddress == providedFingerpirnt.IPAddress)
            {
                trustScore += 25;
            }
            else
            {
                trustScore -= 10;
            }
            if (databaseFingerprint.Language == providedFingerpirnt.Language)
            {
                trustScore += 25;
            }
            else
            {
                trustScore -= 5;
            }
            if (databaseFingerprint.Platform == providedFingerpirnt.Platform)
            {
                trustScore += 25;
            }
            else
            {
                trustScore -= 10;
            }
            if (databaseFingerprint.UserAgent == providedFingerpirnt.UserAgent)
            {
                trustScore += 25;
            }
            else
            {
                trustScore -= 15;
            }
            trustScore = Math.Max(0, Math.Min(maxScore, trustScore));
            return trustScore >= trustScoreThreshold;
        }

        public class LogInAsyncResponse
        {
            public string jwToken { get; set; } = string.Empty;
            public string refreshToken { get; set; } = string.Empty;
            public UserDTO userDTO { get; set; } = new UserDTO();

        }
    }
}
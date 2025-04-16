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
        Task<LogInAsyncResponse> LogInAsync(UserDTO userDTO, FingerprintDTO fingerprintDTO);
        Task<UserDTO> UpdatePasswordAsync(UserDTO dto, string token);
        Task<string> RefreshJWToken(string jwtToken, string refreshToken, FingerprintDTO fingerprintDTO);
        ClaimsPrincipal GetClaimsFromJwt(string token, string secretKey);
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
            _identityData = identityData;
            _configuration = configuration;
        }

        public async Task<LogInAsyncResponse> LogInAsync(UserDTO userDTO, FingerprintDTO fingerprintDTO)
        {
            var user = await _coreData.Users.FirstOrDefaultAsync(x => x.EMail == userDTO.EMail);
            if (user == null) throw new ArgumentException();

            var passwordModel = await _identityData.Passwords
                .Where(x => x.UserId == user.UserId)
                .OrderByDescending(x => x.CreateDate)
                .FirstOrDefaultAsync();
            if (passwordModel == null) throw new Exception(nameof(passwordModel));

            var hashedPassword = IdentityTool.HashPassword(userDTO.Password, passwordModel.Salt);
            if (hashedPassword != passwordModel.HashedPassword) throw new ArgumentException();

            var session = new SessionModel
            {
                CreateDate = DateTime.UtcNow,
                UserId = user.UserId
            };
            var sessionAction = new SessionActionModel
            {
                CreateDate = DateTime.UtcNow,
                SessionAction = SessionActionEnum.LogIn
            };
            var refreshTokenModel = CreateRefreshTokenModel(user.UserId, fingerprintDTO);
            CryptographyTool.Encrypt(refreshTokenModel.Token, out string encryptedRefreshToken);

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

                foreach (var token in existingRefreshTokens)
                {
                    token.DeleteDate = DateTime.UtcNow;
                    _identityData.RefreshTokens.Update(token);
                }
                await _identityData.SaveChangesAsync();

                await _identityData.RefreshTokens.AddAsync(refreshTokenModel);
                await _identityData.SaveChangesAsync();

                scope.Complete();
            }

            var claims = new[]
            {
                new Claim(SessionClaimEnum.UserId.ToString(), user.UserId.ToString()),
                new Claim(SessionClaimEnum.SessionId.ToString(), session.SessionId.ToString())
            };

            var jwt = IdentityTool.GenerateJWTToken(claims, _configuration["Jwt:Key"]!, _configuration["Jwt:Issuer"]!, _configuration["Jwt:Audience"]!, _configuration["Jwt:ExpiresInMinutes"]!);

            _logger.LogAudit($"User Id: {user.UserId}\nIP Address: {fingerprintDTO.IPAddress}UserAgent: {fingerprintDTO.UserAgent}Platform: {fingerprintDTO.Platform}Language: {fingerprintDTO.Language}");

            userDTO.CreateNewRecordFromModel(user);
            userDTO.IsTempPassword = passwordModel.Temporary;

            return new LogInAsyncResponse
            {
                jwToken = jwt,
                refreshToken = encryptedRefreshToken,
                userDTO = userDTO
            };
        }

        public async Task<UserDTO> UpdatePasswordAsync(UserDTO dto, string token)
        {
            if (!IdentityTool.IsValidPassword(dto.Password)) throw new ArgumentException();

            var principal = GetClaimsFromJwt(token, _configuration["Jwt:Key"]!);
            var userId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.UserId.ToString())?.Value);

            var userModel = await _coreData.Users.FirstOrDefaultAsync(x => x.UserId == userId);
            if (userModel == null) throw new Exception(nameof(userId));

            var recentPasswords = await _identityData.Passwords
                .Where(x => x.UserId == userModel.UserId)
                .OrderByDescending(x => x.CreateDate)
                .Take(5)
                .ToListAsync();

            if (!recentPasswords.Any()) throw new Exception(nameof(recentPasswords));

            foreach (var p in recentPasswords)
            {
                if (p.HashedPassword == IdentityTool.HashPassword(dto.Password, p.Salt))
                    throw new ArgumentException();
            }

            var salt = IdentityTool.GenerateSaltString();
            var newPassword = new PasswordModel
            {
                CreateDate = DateTime.UtcNow,
                HashedPassword = IdentityTool.HashPassword(dto.Password, salt),
                Salt = salt,
                UserId = userModel.UserId
            };
            var userComm = new UserCommunicationModel
            {
                CreateDate = DateTime.UtcNow,
                UserId = userModel.UserId,
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
                .Where(x => x.UserId == userId && x.DeleteDate == null && DateTime.UtcNow < x.ExpiresDate)
                .OrderByDescending(x => x.CreateDate)
                .FirstOrDefaultAsync();

            if (refreshTokenModel == null) throw new ArgumentException();

            var existingFingerprint = new FingerprintDTO
            {
                IPAddress = refreshTokenModel.IPAddress,
                Language = refreshTokenModel.Language,
                Platform = refreshTokenModel.Platform,
                UserAgent = refreshTokenModel.UserAgent
            };

            if (!IsFingerprintTrustworthy(existingFingerprint, fingerprintDTO)) throw new ArgumentException();

            CryptographyTool.Decrypt(refreshToken, out string decryptedToken);
            if (refreshTokenModel.Token != decryptedToken) throw new ArgumentException();

            refreshTokenModel.ExpiresDate = refreshTokenModel.ExpiresDate.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]!));
            _identityData.RefreshTokens.Update(refreshTokenModel);
            await _identityData.SaveChangesAsync();

            var claims = new[]
            {
                new Claim(SessionClaimEnum.UserId.ToString(), userId.ToString()),
                new Claim(SessionClaimEnum.SessionId.ToString(), sessionId)
            };

            return IdentityTool.GenerateJWTToken(claims, _configuration["Jwt:Key"]!, _configuration["Jwt:Issuer"]!, _configuration["Jwt:Audience"]!, "-1");
        }

        public ClaimsPrincipal GetClaimsFromJwt(string token, string secretKey)
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

        private bool IsFingerprintTrustworthy(FingerprintDTO db, FingerprintDTO provided)
        {
            var score = 0;
            if (db.IPAddress == provided.IPAddress) score += 25; else score -= 10;
            if (db.Language == provided.Language) score += 25; else score -= 5;
            if (db.Platform == provided.Platform) score += 25; else score -= 10;
            if (db.UserAgent == provided.UserAgent) score += 25; else score -= 15;

            return Math.Clamp(score, 0, 100) >= 80;
        }

        private RefreshTokenModel CreateRefreshTokenModel(long userId, FingerprintDTO fp) => new()
        {
            CreateDate = DateTime.UtcNow,
            ExpiresDate = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]!)),
            Token = IdentityTool.GenerateRefreshToken(),
            UserId = userId,
            IPAddress = fp.IPAddress,
            Language = fp.Language,
            Platform = fp.Platform,
            UserAgent = fp.UserAgent
        };

        public class LogInAsyncResponse
        {
            public string jwToken { get; set; } = string.Empty;
            public string refreshToken { get; set; } = string.Empty;
            public UserDTO userDTO { get; set; } = new();
        }
    }
}
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
using System.Security.Claims;
using System.Transactions;
using static AMWebAPI.Services.IdentityServices.IdentityService;

namespace AMWebAPI.Services.IdentityServices
{
    public interface IIdentityService
    {
        Task<LogInAsyncResponse> LogInAsync(ProviderDTO providerDto, FingerprintDTO fingerprintDTO);
        Task<ProviderDTO> UpdatePasswordAsync(ProviderDTO dto, string token);
        Task<string> RefreshJWToken(string jwtToken, string refreshToken, FingerprintDTO fingerprintDTO);
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

        public async Task<LogInAsyncResponse> LogInAsync(ProviderDTO providerDto, FingerprintDTO fingerprintDTO)
        {
            var provider = await _coreData.Providers.FirstOrDefaultAsync(x => x.EMail == providerDto.EMail);
            if (provider == null) throw new ArgumentException();

            var passwordModel = await _identityData.Passwords
                .Where(x => x.ProviderId == provider.ProviderId)
                .OrderByDescending(x => x.CreateDate)
                .FirstOrDefaultAsync();
            if (passwordModel == null) throw new Exception(nameof(passwordModel));

            var hashedPassword = IdentityTool.HashPassword(providerDto.Password, passwordModel.Salt);
            if (hashedPassword != passwordModel.HashedPassword) throw new ArgumentException();

            var session = new SessionModel
            {
                CreateDate = DateTime.UtcNow,
                ProviderId = provider.ProviderId
            };
            var sessionAction = new SessionActionModel
            {
                CreateDate = DateTime.UtcNow,
                SessionAction = SessionActionEnum.LogIn
            };
            var refreshTokenModel = CreateRefreshTokenModel(provider.ProviderId, fingerprintDTO);
            CryptographyTool.Encrypt(refreshTokenModel.Token, out string encryptedRefreshToken);

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await _coreData.Sessions.AddAsync(session);
                await _coreData.SaveChangesAsync();

                sessionAction.SessionId = session.SessionId;
                await _coreData.SessionActions.AddAsync(sessionAction);
                await _coreData.SaveChangesAsync();

                var existingRefreshTokens = await _identityData.RefreshTokens
                    .Where(x => x.ProviderId == provider.ProviderId && x.DeleteDate == null)
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

            _logger.LogAudit($"Provider Id: {provider.ProviderId}");

            var claims = new[]
            {
                new Claim(SessionClaimEnum.ProviderId.ToString(), provider.ProviderId.ToString()),
                new Claim(SessionClaimEnum.SessionId.ToString(), session.SessionId.ToString())
            };

            var jwt = IdentityTool.GenerateJWTToken(claims, _configuration["Jwt:Key"]!, _configuration["Jwt:Issuer"]!, _configuration["Jwt:Audience"]!, _configuration["Jwt:ExpiresInMinutes"]!);

            _logger.LogAudit($"Provider Id: {provider.ProviderId}\nIP Address: {fingerprintDTO.IPAddress}UserAgent: {fingerprintDTO.UserAgent}Platform: {fingerprintDTO.Platform}Language: {fingerprintDTO.Language}");

            providerDto.CreateNewRecordFromModel(provider);
            providerDto.IsTempPassword = passwordModel.Temporary;

            return new LogInAsyncResponse
            {
                jwToken = jwt,
                refreshToken = encryptedRefreshToken,
                providerDTO = providerDto
            };
        }

        public async Task<ProviderDTO> UpdatePasswordAsync(ProviderDTO dto, string token)
        {
            if (!IdentityTool.IsValidPassword(dto.Password)) throw new ArgumentException();

            var principal = IdentityTool.GetClaimsFromJwt(token, _configuration["Jwt:Key"]!);
            var providerId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value);
            var sessionId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.SessionId.ToString())?.Value);

            var providerModel = await _coreData.Providers.FirstOrDefaultAsync(x => x.ProviderId == providerId);
            if (providerModel == null) throw new Exception(nameof(providerId));

            var recentPasswords = await _identityData.Passwords
                .Where(x => x.ProviderId == providerModel.ProviderId)
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
                ProviderId = providerModel.ProviderId
            };
            var providerComm = new ProviderCommunicationModel
            {
                CreateDate = DateTime.UtcNow,
                ProviderId = providerModel.ProviderId,
                Message = "Your password was recently changed. If you did not request this change please change your password immediately or contact customer service."
            };

            var sessionAction = new SessionActionModel()
            {
                CreateDate = DateTime.UtcNow,
                SessionAction = SessionActionEnum.ChangePassword,
                SessionId = sessionId
            };

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await _coreData.AddAsync(providerComm);
                await _coreData.SaveChangesAsync();

                await _coreData.SessionActions.AddAsync(sessionAction);
                await _coreData.SaveChangesAsync();

                await _identityData.Passwords.AddAsync(newPassword);
                await _identityData.SaveChangesAsync();

                scope.Complete();
            }

            dto.CreateNewRecordFromModel(providerModel);
            _logger.LogAudit($"Provider Id: {providerId}");
            return dto;
        }

        public async Task<string> RefreshJWToken(string jwtToken, string refreshToken, FingerprintDTO fingerprintDTO)
        {
            var principal = IdentityTool.GetClaimsFromJwt(jwtToken, _configuration["Jwt:Key"]!);
            var providerId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value);
            var sessionId = principal.FindFirst(SessionClaimEnum.SessionId.ToString())?.Value;

            var refreshTokenModel = await _identityData.RefreshTokens
                .Where(x => x.ProviderId == providerId && x.DeleteDate == null && DateTime.UtcNow < x.ExpiresDate)
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
                new Claim(SessionClaimEnum.ProviderId.ToString(), providerId.ToString()),
                new Claim(SessionClaimEnum.SessionId.ToString(), sessionId)
            };

            return IdentityTool.GenerateJWTToken(claims, _configuration["Jwt:Key"]!, _configuration["Jwt:Issuer"]!, _configuration["Jwt:Audience"]!, "-1");
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

        private RefreshTokenModel CreateRefreshTokenModel(long providerId, FingerprintDTO fp) => new()
        {
            CreateDate = DateTime.UtcNow,
            ExpiresDate = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]!)),
            Token = IdentityTool.GenerateRefreshToken(),
            ProviderId = providerId,
            IPAddress = fp.IPAddress,
            Language = fp.Language,
            Platform = fp.Platform,
            UserAgent = fp.UserAgent
        };

        public class LogInAsyncResponse
        {
            public string jwToken { get; set; } = string.Empty;
            public string refreshToken { get; set; } = string.Empty;
            public ProviderDTO providerDTO { get; set; } = new();
        }
    }
}
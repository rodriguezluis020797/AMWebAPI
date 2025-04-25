using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMData.Models.IdentityModels;
using AMTools;
using AMTools.Tools;
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
        Task<BaseDTO> UpdatePasswordAsync(ProviderDTO dto, string token);
        Task ResetPasswordAsync(ProviderDTO dto);
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
        public async Task<LogInAsyncResponse> LogInAsync(ProviderDTO dto, FingerprintDTO fingerprintDTO)
        {
            var provider = await _coreData.Providers.FirstOrDefaultAsync(x => x.EMail == dto.EMail)
                          ?? throw new ArgumentException(nameof(dto.EMail));

            var passwordModel = await _identityData.Passwords
                .Where(x => x.ProviderId == provider.ProviderId)
                .OrderByDescending(x => x.CreateDate)
                .FirstOrDefaultAsync() ?? throw new Exception(nameof(provider.ProviderId));

            var hashedPassword = IdentityTool.HashPassword(dto.CurrentPassword, passwordModel.Salt);
            if (!string.Equals(hashedPassword, passwordModel.HashedPassword))
                throw new ArgumentException();

            var session = new SessionModel(provider.ProviderId);

            var sessionAction = new SessionActionModel(0, SessionActionEnum.LogIn);

            var refreshTokenModel = CreateRefreshTokenModel(provider.ProviderId, IdentityTool.GenerateRefreshToken(), fingerprintDTO);

            CryptographyTool.Encrypt(refreshTokenModel.Token, out string encryptedRefreshToken);

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await _coreData.Sessions.AddAsync(session);
                await _coreData.SaveChangesAsync();

                sessionAction.SessionId = session.SessionId;

                await _coreData.SessionActions.AddAsync(sessionAction);
                await _coreData.SaveChangesAsync();

                var deleteExistingTokens = _identityData.RefreshTokens
                   .Where(x => x.ProviderId == provider.ProviderId && x.DeleteDate == null)
                   .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));

                var addNewToken = _identityData.RefreshTokens.AddAsync(refreshTokenModel).AsTask();

                await Task.WhenAll(deleteExistingTokens, addNewToken);

                await _identityData.SaveChangesAsync();

                scope.Complete();
            }

            _logger.LogAudit($"Provider Id: {provider.ProviderId}");

            var claims = new[]
            {
                new Claim(SessionClaimEnum.ProviderId.ToString(), provider.ProviderId.ToString()),
                new Claim(SessionClaimEnum.SessionId.ToString(), session.SessionId.ToString())
            };

            var jwt = IdentityTool.GenerateJWTToken(
                claims,
                _configuration["Jwt:Key"]!,
                _configuration["Jwt:Issuer"]!,
                _configuration["Jwt:Audience"]!,
                _configuration["Jwt:ExpiresInMinutes"]!
            );

            _logger.LogAudit($"Provider Id: {provider.ProviderId}\nIP Address: {fingerprintDTO.IPAddress} UserAgent: {fingerprintDTO.UserAgent} Platform: {fingerprintDTO.Platform} Language: {fingerprintDTO.Language}");


            return new LogInAsyncResponse()
            {
                baseDTO = new BaseDTO()
                {
                    IsSpecialCase = passwordModel.Temporary
                },
                jwToken = jwt,
                refreshToken = refreshTokenModel.Token
            };
        }

        public async Task<BaseDTO> UpdatePasswordAsync(ProviderDTO dto, string token)
        {
            var response = new BaseDTO();

            if (!IdentityTool.IsValidPassword(dto.NewPassword))
                throw new ArgumentException("Password does not meet complexity requirements.");

            var principal = IdentityTool.GetClaimsFromJwt(token, _configuration["Jwt:Key"]!);
            var providerId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value);
            var sessionId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.SessionId.ToString())?.Value);

            var providerModel = await _coreData.Providers
                .FirstOrDefaultAsync(x => x.ProviderId == providerId)
                ?? throw new Exception($"Provider not found for ID: {providerId}");


            if (!dto.IsTempPassword)
            {
                var currentPassword = await _identityData.Passwords
                    .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
                    .OrderByDescending(x => x.CreateDate)
                    .FirstOrDefaultAsync();

                var currentSalt = currentPassword.Salt;

                var givenCurrentPasswordHash = IdentityTool.HashPassword(dto.CurrentPassword, currentPassword.Salt);

                if (!string.Equals(currentPassword.HashedPassword, givenCurrentPasswordHash))
                    throw new ArgumentException("Current password does not match.");
            }

            var recentPasswords = await _identityData.Passwords
                .Where(x => x.ProviderId == providerId)
                .ToListAsync();

            foreach (var password in recentPasswords)
            {
                var hash = IdentityTool.HashPassword(dto.NewPassword, password.Salt);
                if (string.Equals(hash, password.HashedPassword))
                    throw new ArgumentException("Password was recently used.");
            }

            var salt = IdentityTool.GenerateSaltString();

            var newPassword = new PasswordModel(providerId, false, IdentityTool.HashPassword(dto.NewPassword, salt), salt);

            var message = "Your password was recently changed. " +
                "If you did not request this change, please change your password immediately or contact customer service.";
            var providerComm = new ProviderCommunicationModel(providerId, message, DateTime.MinValue);

            var sessionAction = new SessionActionModel(sessionId, SessionActionEnum.ChangePassword);

            var maxRetries = 3;
            var retryDelay = TimeSpan.FromSeconds(2);
            var attempt = 0;

            while (true)
            {
                try
                {
                    using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                    {
                        await _identityData.Passwords
                            .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
                            .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));

                        await _coreData.ProviderCommunications.AddAsync(providerComm);
                        await _coreData.SessionActions.AddAsync(sessionAction);
                        await _coreData.SaveChangesAsync();

                        await _identityData.Passwords.AddAsync(newPassword);
                        await _identityData.SaveChangesAsync();

                        scope.Complete();
                    }

                    _logger.LogInfo("All database changes completed successfully.");
                    break;
                }
                catch (Exception ex)
                {
                    attempt++;
                    _logger.LogError($"Attempt {attempt} failed: {ex.Message}");

                    if (attempt >= maxRetries)
                    {
                        _logger.LogError("All attempts failed. No data was committed.");
                        throw;
                    }

                    await Task.Delay(retryDelay);
                }
            }

            _logger.LogAudit($"Provider Id: {providerId}");

            return response;
        }

        public async Task<string> RefreshJWToken(string jwtToken, string refreshToken, FingerprintDTO fingerprintDTO)
        {
            var principal = IdentityTool.GetClaimsFromJwt(jwtToken, _configuration["Jwt:Key"]!);
            var providerId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value);
            var sessionId = principal.FindFirst(SessionClaimEnum.SessionId.ToString())?.Value;

            var refreshTokenModel = await _identityData.RefreshTokens
                .Where(x => x.ProviderId == providerId && x.DeleteDate == null && DateTime.UtcNow < x.ExpiresDate)
                .OrderByDescending(x => x.CreateDate)
                .FirstOrDefaultAsync()
                ?? throw new ArgumentException();

            var existingFingerprint = new FingerprintDTO
            {
                IPAddress = refreshTokenModel.IPAddress,
                Language = refreshTokenModel.Language,
                Platform = refreshTokenModel.Platform,
                UserAgent = refreshTokenModel.UserAgent
            };

            if (!IsFingerprintTrustworthy(existingFingerprint, fingerprintDTO))
                throw new ArgumentException();

            CryptographyTool.Decrypt(refreshToken, out string decryptedToken);
            if (!string.Equals(refreshTokenModel.Token, decryptedToken))
                throw new ArgumentException();

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

        public async Task ResetPasswordAsync(ProviderDTO dto)
        {
            var response = new ProviderDTO();

            var provider = await _coreData.Providers
                .Where(x => x.EMail.Equals(dto.EMail))
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (provider == null)
            {
                return;
            }

            var tempPasswordString = IdentityTool.GenerateRandomPassword();
            var saltString = IdentityTool.GenerateSaltString();
            var tempPasswordHashString = IdentityTool.HashPassword(tempPasswordString, saltString);

            var message =
                $"A password reset has been requested for your account.\n" +
                $"If you did not request a password reset, please update your password as soon as possible.\n" +
                $"Temporary password: {tempPasswordString}";

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {

                await _identityData.Passwords
                .Where(x => x.ProviderId == provider.ProviderId && x.DeleteDate == null)
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));
                await _identityData.SaveChangesAsync();

                await _identityData.Passwords.AddAsync(new PasswordModel(provider.ProviderId, true, tempPasswordHashString, saltString));
                await _identityData.SaveChangesAsync();

                await _coreData.ProviderCommunications.AddAsync(new ProviderCommunicationModel(provider.ProviderId, message, DateTime.MinValue));
                await _coreData.SaveChangesAsync();

                scope.Complete();
            }
        }

        private bool IsFingerprintTrustworthy(FingerprintDTO databaseFingerprint, FingerprintDTO providedFingerprint)
        {
            var score = 0;
            if (databaseFingerprint.IPAddress == providedFingerprint.IPAddress) score += 25; else score -= 10;
            if (databaseFingerprint.Language == providedFingerprint.Language) score += 25; else score -= 5;
            if (databaseFingerprint.Platform == providedFingerprint.Platform) score += 25; else score -= 10;
            if (databaseFingerprint.UserAgent == providedFingerprint.UserAgent) score += 25; else score -= 15;

            return Math.Clamp(score, 0, 100) >= 80;
        }

        private RefreshTokenModel CreateRefreshTokenModel(long providerId, string token, FingerprintDTO fingerprintDTO)
            => new(providerId, token, fingerprintDTO.IPAddress, fingerprintDTO.UserAgent, fingerprintDTO.Platform, fingerprintDTO.Language, DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]!)));

        public class LogInAsyncResponse
        {
            public string jwToken { get; set; } = string.Empty;
            public string refreshToken { get; set; } = string.Empty;
            public BaseDTO baseDTO { get; set; } = new();
        }
    }
}
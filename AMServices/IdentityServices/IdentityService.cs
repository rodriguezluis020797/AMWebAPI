using System.Security.Claims;
using System.Transactions;
using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMData.Models.IdentityModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMServices.IdentityServices;

public interface IIdentityService
{
    Task<LogInAsyncResponse> LogInAsync(ProviderDTO providerDto, FingerprintDTO fingerprintDto);
    Task<BaseDTO> UpdatePasswordAsync(ProviderDTO providerDto, string jwt);
    Task<BaseDTO> ResetPasswordAsync(ProviderDTO providerDto);
    Task<string> RefreshJWT(string jwt, string refreshToken, FingerprintDTO fingerprintDto);
}

public class IdentityService : IIdentityService
{
    private readonly IConfiguration _config;
    private readonly AMCoreData _coreData;
    private readonly AMIdentityData _identityData;
    private readonly IAMLogger _logger;

    public IdentityService(AMCoreData coreData, AMIdentityData identityData, IAMLogger logger,
        IConfiguration config)
    {
        _coreData = coreData;
        _identityData = identityData;
        _logger = logger;
        _config = config;
    }

    public async Task<LogInAsyncResponse> LogInAsync(ProviderDTO dto, FingerprintDTO fingerprintDTO)
    {
        var provider = await _coreData.Providers.FirstOrDefaultAsync(x => x.EMail == dto.EMail)
                       ?? throw new ArgumentException(nameof(dto.EMail));

        var passwordModel = await _identityData.Passwords
                                .Where(x => x.ProviderId == provider.ProviderId)
                                .OrderByDescending(x => x.CreateDate)
                                .FirstOrDefaultAsync()
                            ?? throw new Exception(nameof(provider.ProviderId));

        var hashedPassword = IdentityTool.HashPassword(dto.CurrentPassword, passwordModel.Salt);
        if (!string.Equals(hashedPassword, passwordModel.HashedPassword))
            throw new ArgumentException("Incorrect password.");

        var session = new SessionModel(provider.ProviderId);
        var sessionAction = new SessionActionModel(0, SessionActionEnum.LogIn);
        var refreshTokenModel = CreateRefreshTokenModel(provider.ProviderId, IdentityTool.GenerateRefreshToken(),
            fingerprintDTO);

        await ExecuteWithRetryAsync(async () =>
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            await _coreData.Sessions.AddAsync(session);
            await _coreData.SaveChangesAsync();

            sessionAction.SessionId = session.SessionId;
            await _coreData.SessionActions.AddAsync(sessionAction);
            await _coreData.SaveChangesAsync();

            var deleteOldTokens = _identityData.RefreshTokens
                .Where(x => x.ProviderId == provider.ProviderId && x.DeleteDate == null)
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));
            var addNewToken = _identityData.RefreshTokens.AddAsync(refreshTokenModel).AsTask();

            await Task.WhenAll(deleteOldTokens, addNewToken);
            await _identityData.SaveChangesAsync();

            scope.Complete();
        });

        _logger.LogAudit($"Provider Id: {provider.ProviderId}");
        _logger.LogAudit(
            $"Login details: IP={fingerprintDTO.IPAddress}, UA={fingerprintDTO.UserAgent}, Platform={fingerprintDTO.Platform}, Language={fingerprintDTO.Language}");

        CryptographyTool.Encrypt(refreshTokenModel.Token, out var encryptedRefreshToken);

        return new LogInAsyncResponse
        {
            ProviderDto = new ProviderDTO
            {
                IsSpecialCase = passwordModel.Temporary,
                HasCompletedSignUp = provider.CountryCode != CountryCodeEnum.Select &&
                                     provider.StateCode != StateCodeEnum.Select &&
                                     provider.TimeZoneCode != TimeZoneCodeEnum.Select
            },
            Jwt = GenerateJwt(provider.ProviderId, session.SessionId),
            RefreshToken = encryptedRefreshToken
        };
    }

    public async Task<BaseDTO> UpdatePasswordAsync(ProviderDTO dto, string jwt)
    {
        var response = new BaseDTO { IsSpecialCase = dto.IsTempPassword };

        if (!IdentityTool.IsValidPassword(dto.NewPassword))
            throw new ArgumentException("Password does not meet complexity requirements.");

        var providerId =
            IdentityTool.GetJwtClaimById(jwt, _config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());
        var sessionId =
            IdentityTool.GetJwtClaimById(jwt, _config["Jwt:Key"]!, SessionClaimEnum.SessionId.ToString());

        if (!dto.IsTempPassword)
        {
            var currentPassword = await _identityData.Passwords
                .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
                .OrderByDescending(x => x.CreateDate)
                .FirstOrDefaultAsync();

            if (currentPassword == null ||
                IdentityTool.HashPassword(dto.CurrentPassword, currentPassword.Salt) !=
                currentPassword.HashedPassword)
                throw new ArgumentException("Current password does not match.");
        }

        var recentPasswords = await _identityData.Passwords
            .Where(x => x.ProviderId == providerId)
            .ToListAsync();

        if (recentPasswords.Any(p =>
                IdentityTool.HashPassword(dto.NewPassword, p.Salt) == p.HashedPassword))
            throw new ArgumentException("Password was recently used.");

        var salt = IdentityTool.GenerateSaltString();
        var newPassword =
            new PasswordModel(providerId, false, IdentityTool.HashPassword(dto.NewPassword, salt), salt);

        var sessionAction = new SessionActionModel(sessionId, SessionActionEnum.ChangePassword);
        var providerComm = new ProviderCommunicationModel(providerId,
            "Your password was recently changed. If you did not request this change, please contact support.",
            DateTime.MinValue);

        await ExecuteWithRetryAsync(async () =>
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            await _identityData.Passwords
                .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));

            await _identityData.Passwords.AddAsync(newPassword);
            await _coreData.SessionActions.AddAsync(sessionAction);
            await _coreData.ProviderCommunications.AddAsync(providerComm);

            await _coreData.SaveChangesAsync();
            await _identityData.SaveChangesAsync();

            scope.Complete();
        });

        _logger.LogAudit($"Provider Id: {providerId}");
        return response;
    }

    public async Task<string> RefreshJWT(string jwt, string refreshToken, FingerprintDTO fingerprintDTO)
    {
        var providerId =
            IdentityTool.GetJwtClaimById(jwt, _config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());
        var sessionId =
            IdentityTool.GetJwtClaimById(jwt, _config["Jwt:Key"]!, SessionClaimEnum.SessionId.ToString());

        var refreshTokenModel = await _identityData.RefreshTokens
                                    .Where(x => x.ProviderId == providerId && x.DeleteDate == null &&
                                                DateTime.UtcNow < x.ExpiresDate)
                                    .FirstOrDefaultAsync()
                                ?? throw new ArgumentException("Refresh token not found or expired.");

        var storedFingerprint = new FingerprintDTO
        {
            IPAddress = refreshTokenModel.IPAddress,
            Language = refreshTokenModel.Language,
            Platform = refreshTokenModel.Platform,
            UserAgent = refreshTokenModel.UserAgent
        };

        if (!IsFingerprintTrustworthy(storedFingerprint, fingerprintDTO))
            throw new ArgumentException("Fingerprint mismatch.");

        CryptographyTool.Decrypt(refreshToken, out var decryptedToken);
        if (refreshTokenModel.Token != decryptedToken)
            throw new ArgumentException("Refresh token mismatch.");

        refreshTokenModel.ExpiresDate = refreshTokenModel.ExpiresDate
            .AddDays(int.Parse(_config["Jwt:RefreshTokenExpirationDays"]!));
        _identityData.RefreshTokens.Update(refreshTokenModel);
        await _identityData.SaveChangesAsync();

        return GenerateJwt(providerId, sessionId);
    }

    public async Task<BaseDTO> ResetPasswordAsync(ProviderDTO dto)
    {
        var response = new BaseDTO();

        var provider = await _coreData.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EMail == dto.EMail);
        if (provider == null) return response;

        var tempPassword = IdentityTool.GenerateRandomPassword();
        var salt = IdentityTool.GenerateSaltString();
        var hashed = IdentityTool.HashPassword(tempPassword, salt);

        var message = $"A password reset has been requested for your account.\nTemporary password: {tempPassword}";

        await ExecuteWithRetryAsync(async () =>
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            await _identityData.Passwords
                .Where(x => x.ProviderId == provider.ProviderId && x.DeleteDate == null)
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));
            await _identityData.SaveChangesAsync();

            await _identityData.Passwords.AddAsync(new PasswordModel(provider.ProviderId, true, hashed, salt));
            await _coreData.ProviderCommunications.AddAsync(
                new ProviderCommunicationModel(provider.ProviderId, message, DateTime.MinValue));

            await _identityData.SaveChangesAsync();
            await _coreData.SaveChangesAsync();

            scope.Complete();
        });

        return response;
    }

    private async Task ExecuteWithRetryAsync(Func<Task> operation)
    {
        const int maxRetries = 3;
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
            try
            {
                await operation();
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogError($"Attempt {attempt} failed: {ex.Message}");
                await Task.Delay(delay);
            }

        _logger.LogError("All attempts failed. No data was committed.");
        throw new InvalidOperationException("Operation failed after multiple retries.");
    }

    private string GenerateJwt(long providerId, long sessionId)
    {
        var claims = new[]
        {
            new Claim(SessionClaimEnum.ProviderId.ToString(), providerId.ToString()),
            new Claim(SessionClaimEnum.SessionId.ToString(), sessionId.ToString())
        };

        return IdentityTool.GenerateJWTToken(
            claims,
            _config["Jwt:Key"]!,
            _config["Jwt:Issuer"]!,
            _config["Jwt:Audience"]!,
            _config["Jwt:ExpiresInMinutes"]!
        );
    }

    private bool IsFingerprintTrustworthy(FingerprintDTO db, FingerprintDTO provided)
    {
        var score = 0;
        if (db.IPAddress == provided.IPAddress) score += 25;
        else score -= 10;
        if (db.Language == provided.Language) score += 25;
        else score -= 5;
        if (db.Platform == provided.Platform) score += 25;
        else score -= 10;
        if (db.UserAgent == provided.UserAgent) score += 25;
        else score -= 15;

        return Math.Clamp(score, 0, 100) >= 80;
    }

    private RefreshTokenModel CreateRefreshTokenModel(long providerId, string token, FingerprintDTO fp)
    {
        return new RefreshTokenModel(providerId, token, fp.IPAddress, fp.UserAgent, fp.Platform, fp.Language,
            DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenExpirationDays"]!)));
    }
}

public class LogInAsyncResponse
{
    public string Jwt { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public ProviderDTO ProviderDto { get; set; } = new();
}
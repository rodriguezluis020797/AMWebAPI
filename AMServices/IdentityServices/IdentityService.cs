using System.Security.Claims;
using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMData.Models.IdentityModels;
using AMServices.DataServices;
using AMTools;
using MCCDotnetTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMServices.IdentityServices;

public interface IIdentityService
{
    Task<LogInResponseDTO> LogInAsync(ProviderDTO providerDto, FingerprintDTO fingerprintDto);
    Task<BaseDTO> UpdatePasswordAsync(ProviderDTO providerDto, string jwt);
    Task<BaseDTO> ResetPasswordRequestAsync(ProviderDTO providerDto);
    Task<BaseDTO> ResetPasswordAsync(ProviderDTO providerDto, string guid);
    Task<string> RefreshJWT(string jwt, string refreshToken, FingerprintDTO fingerprintDto);
}

public class IdentityService(
    AMCoreData coreData,
    AMIdentityData identityData,
    IAMLogger logger,
    IConfiguration config)
    : IIdentityService
{
    public async Task<LogInResponseDTO> LogInAsync(ProviderDTO dto, FingerprintDTO fingerprintDTO)
    {
        var provider = new ProviderModel();
        await coreData.ExecuteWithRetryAsync(async () =>
        {
            provider = await coreData.Providers.FirstOrDefaultAsync(x => x.EMail == dto.EMail);
        });

        if (provider == null) throw new ArgumentException(nameof(dto.EMail));

        if (provider.AccessGranted == false) throw new ArgumentException(nameof(provider.AccessGranted));

        var passwordModel = new PasswordModel();
        await coreData.ExecuteWithRetryAsync(async () =>
        {
            passwordModel = await identityData.Passwords
                                .Where(x => x.ProviderId == provider.ProviderId)
                                .OrderByDescending(x => x.CreateDate)
                                .FirstOrDefaultAsync()
                            ?? throw new Exception(nameof(provider.ProviderId));
        });


        var hashedPassword = IdentityTool.HashPassword(dto.CurrentPassword, passwordModel.Salt);
        if (!string.Equals(hashedPassword, passwordModel.HashedPassword))
            throw new ArgumentException("Incorrect password.");

        var session = new SessionModel(provider.ProviderId);
        var sessionAction = new SessionActionModel(SessionActionEnum.LogIn);
        var refreshTokenModel = CreateRefreshTokenModel(provider.ProviderId, IdentityTool.GenerateRefreshToken(),
            fingerprintDTO);

        await coreData.ExecuteWithRetryAsync(async () =>
        {
            await coreData.Sessions.AddAsync(session);
            await coreData.SaveChangesAsync();

            sessionAction.SessionId = session.SessionId;
            await coreData.SessionActions.AddAsync(sessionAction);
            await coreData.SaveChangesAsync();

            var deleteOldTokens = identityData.RefreshTokens
                .Where(x => x.ProviderId == provider.ProviderId && x.DeleteDate == null)
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));
            var addNewToken = identityData.RefreshTokens.AddAsync(refreshTokenModel).AsTask();

            await Task.WhenAll(deleteOldTokens, addNewToken);
            await identityData.SaveChangesAsync();
        });

        logger.LogAudit(
            $"Provider Id: {provider.ProviderId} - Login details: IP = {fingerprintDTO.IPAddress} - User Agent = {fingerprintDTO.UserAgent} - Platform = {fingerprintDTO.Platform} - Language = {fingerprintDTO.Language}");

        MCCCryptographyTool.Encrypt(refreshTokenModel.Token, out var encryptedRefreshToken, config["Cryptography:Key"]!, config["Cryptography:IV"]!);

        return new LogInResponseDTO
        {
            ProviderDto = new ProviderDTO
            {
                IsSpecialCase = passwordModel.Temporary,
                AccountStatus = provider.AccountStatus
            },
            Jwt = GenerateJwt(provider.ProviderId, session.SessionId),
            RefreshToken = encryptedRefreshToken,
            AccountStatus = provider.AccountStatus
        };
    }

    public async Task<BaseDTO> UpdatePasswordAsync(ProviderDTO dto, string jwt)
    {
        var response = new BaseDTO { IsSpecialCase = dto.IsTempPassword };

        if (!IdentityTool.IsValidPassword(dto.NewPassword))
            throw new ArgumentException("Password does not meet complexity requirements.");

        var providerId =
            IdentityTool
                .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        var sessionId =
            IdentityTool
                .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.SessionId));

        if (!dto.IsTempPassword)
        {
            var currentPassword = new PasswordModel();

            await coreData.ExecuteWithRetryAsync(async () =>
            {
                currentPassword = await identityData.Passwords
                    .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
                    .OrderByDescending(x => x.CreateDate)
                    .FirstOrDefaultAsync();
            });

            if (currentPassword == null ||
                IdentityTool.HashPassword(dto.CurrentPassword, currentPassword.Salt)
                    .Equals(currentPassword.HashedPassword))
                throw new ArgumentException("Current password does not match.");
        }

        var recentPasswords = new List<PasswordModel>();

        await coreData.ExecuteWithRetryAsync(async () =>
        {
            recentPasswords = await identityData.Passwords
                .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
                .ToListAsync();
        });

        if (recentPasswords.Any(p =>
                IdentityTool.HashPassword(dto.NewPassword, p.Salt) == p.HashedPassword))
            throw new ArgumentException("Password was recently used.");

        var salt = IdentityTool.GenerateSaltString();
        var newPassword =
            new PasswordModel(providerId, false, IdentityTool.HashPassword(dto.NewPassword, salt), salt);

        var sessionAction = new SessionActionModel(SessionActionEnum.ChangePassword)
        {
            SessionId = sessionId
        };
        var providerComm = new ProviderCommunicationModel(providerId,
            "Your password was recently changed. If you did not request this change, please contact support.",
            DateTime.MinValue);

        await coreData.ExecuteWithRetryAsync(async () =>
        {
            await identityData.Passwords
                .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));

            await identityData.Passwords.AddAsync(newPassword);
            await coreData.SessionActions.AddAsync(sessionAction);
            await coreData.ProviderCommunications.AddAsync(providerComm);

            await coreData.SaveChangesAsync();
            await identityData.SaveChangesAsync();
        });

        logger.LogAudit($"Provider Id: {providerId}");
        return response;
    }

    public async Task<BaseDTO> ResetPasswordAsync(ProviderDTO dto, string guid)
    {
        var response = new BaseDTO();
        var request = new ResetPasswordRequestModel();
        var recentPasswords = new List<PasswordModel>();

        await coreData.ExecuteWithRetryAsync(async () =>
        {
            request = await coreData.ResetPasswordRequests
                .Where(x => x.QueryGuid.Equals(guid) && x.DeleteDate == null)
                .FirstOrDefaultAsync();

            if (request == null) throw new Exception(nameof(request));

            recentPasswords = await identityData.Passwords
                .Where(x => x.ProviderId == request.ProviderId && x.DeleteDate == null)
                .ToListAsync();
        });

        if (recentPasswords.Any(p =>
                IdentityTool.HashPassword(dto.NewPassword, p.Salt).Equals(p.HashedPassword)))
        {
            response.ErrorMessage = "Password was recently used.";
            return response;
        }

        var salt = IdentityTool
            .GenerateSaltString();

        var newPassword =
            new PasswordModel(request.ProviderId, false, IdentityTool
                .HashPassword(dto.NewPassword, salt), salt);

        var providerComm = new ProviderCommunicationModel(request.ProviderId,
            "Your password was recently changed. If you did not request this change, please contact support.",
            DateTime.MinValue);

        await coreData.ExecuteWithRetryAsync(async () =>
        {
            await identityData.Passwords
                .Where(x => x.ProviderId == request.ProviderId && x.DeleteDate == null)
                .ExecuteUpdateAsync(upd => upd
                    .SetProperty(x => x.DeleteDate, DateTime.UtcNow));

            await coreData.ResetPasswordRequests
                .Where(x => x.ProviderId == request.ProviderId && x.DeleteDate == null)
                .ExecuteUpdateAsync(upd => upd
                    .SetProperty(x => x.DeleteDate, DateTime.UtcNow));

            await identityData.Passwords
                .AddAsync(newPassword);

            await coreData.ProviderCommunications
                .AddAsync(providerComm);

            await coreData
                .SaveChangesAsync();

            await identityData
                .SaveChangesAsync();
        });

        logger.LogAudit($"Provider Id: {request.ProviderId}");

        return response;
    }

    public async Task<string> RefreshJWT(string jwt, string refreshToken, FingerprintDTO fingerprintDTO)
    {
        var providerId =
            IdentityTool.GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));
        var sessionId =
            IdentityTool.GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.SessionId));

        var refreshTokenModel = await identityData.RefreshTokens
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

        MCCCryptographyTool.Decrypt(refreshToken, out var decryptedToken, config["Cryptography:Key"]!, config["Cryptography:IV"]!);
        if (refreshTokenModel.Token != decryptedToken)
            throw new ArgumentException("Refresh token mismatch.");

        refreshTokenModel.ExpiresDate = DateTime.UtcNow
            .AddDays(int.Parse(config["Jwt:RefreshTokenExpirationDays"]!));
        identityData.RefreshTokens.Update(refreshTokenModel);
        await identityData.SaveChangesAsync();

        return GenerateJwt(providerId, sessionId);
    }

    public async Task<BaseDTO> ResetPasswordRequestAsync(ProviderDTO dto)
    {
        var response = new BaseDTO();
        var provider = new ProviderModel();

        await coreData.ExecuteWithRetryAsync(async () =>
        {
            provider = await coreData.Providers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EMail == dto.EMail);
        });

        if (provider == null) return response;

        var guid = Guid.NewGuid().ToString();

        var message = $"A password reset has been requested for your account.\n" +
                      $"If you did not request this change, please disregard this e-mail.\n" +
                      $"Otherwise, please follow this link to reset your password:\n" +
                      $"{config["URIs:AngularURI"]}/reset-password?guid={guid}";

        await coreData.ExecuteWithRetryAsync(async () =>
        {
            await using var trans = await coreData.Database.BeginTransactionAsync();

            await coreData.ResetPasswordRequests
                .Where(x => x.ProviderId == provider.ProviderId && x.DeleteDate == null)
                .ExecuteUpdateAsync(upd => upd
                    .SetProperty(x => x.DeleteDate, DateTime.UtcNow));

            await coreData.ProviderCommunications.AddAsync(
                new ProviderCommunicationModel(provider.ProviderId, message, DateTime.MinValue));
            await coreData.ResetPasswordRequests.AddAsync(new ResetPasswordRequestModel(provider.ProviderId, guid));

            await coreData.SaveChangesAsync();
            await trans.CommitAsync();
        });

        return response;
    }

    private string GenerateJwt(long providerId, long sessionId)
    {
        var claims = new[]
        {
            new Claim(nameof(SessionClaimEnum.ProviderId), providerId.ToString()),
            new Claim(nameof(SessionClaimEnum.SessionId), sessionId.ToString())
        };

        return IdentityTool.GenerateJWTToken(
            claims,
            config["Jwt:Key"]!,
            config["Jwt:Issuer"]!,
            config["Jwt:Audience"]!,
            config["Jwt:ExpiresInMinutes"]!
        );
    }

    private bool IsFingerprintTrustworthy(FingerprintDTO db, FingerprintDTO provided)
    {
        var score = 0;
        if (db.IPAddress == provided.IPAddress)
        {
            score += 25;
        }
        else
        {
            logger.LogInfo($"IP address mismatch. - Provided: {provided.IPAddress} - Stored: {db.IPAddress}");
            score -= 10;
        }

        if (db.Language == provided.Language)
        {
            score += 25;
        }
        else
        {
            logger.LogInfo($"Language mismatch. - Provided: {provided.Language} - Stored: {db.Language}");
            score -= 5;
        }

        if (db.Platform == provided.Platform)
        {
            score += 25;
        }
        else
        {
            logger.LogInfo($"Platform mismatch. - Provided: {provided.Platform} - Stored: {db.Platform}");
            score -= 10;
        }

        if (db.UserAgent == provided.UserAgent)
        {
            score += 25;
        }
        else
        {
            logger.LogInfo($"User agent mismatch. - Provided: {provided.UserAgent} - Stored: {db.UserAgent}");
            score -= 15;
        }

        return Math.Clamp(score, 0, 100) >= 80;
    }

    private RefreshTokenModel CreateRefreshTokenModel(long providerId, string token, FingerprintDTO fp)
    {
        return new RefreshTokenModel(providerId, token, fp.IPAddress, fp.UserAgent, fp.Platform, fp.Language,
            DateTime.UtcNow.AddDays(int.Parse(config["Jwt:RefreshTokenExpirationDays"]!)));
    }
}

public class LogInResponseDTO
{
    public string Jwt { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public ProviderDTO ProviderDto { get; init; } = new();
    public AccountStatusEnum AccountStatus { get; init; } = AccountStatusEnum.Unknown;
}
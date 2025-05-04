using System.Diagnostics;
using System.Runtime.CompilerServices;
using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMServices.CoreServices;

public interface IProviderService
{
    Task<BaseDTO> CreateProviderAsync(ProviderDTO dto);
    Task<ProviderDTO> GetProviderAsync(string jwt);
    Task<ProviderDTO> UpdateEMailAsync(ProviderDTO dto, string jwt);
    Task<BaseDTO> UpdateProviderAsync(ProviderDTO dto, string jwt);
    Task<BaseDTO> VerifyUpdateEMailAsync(string guid);
}

public class ProviderService(IAMLogger logger, AMCoreData db, IConfiguration config) : IProviderService
{
    public async Task<BaseDTO> CreateProviderAsync(ProviderDTO dto)
    {
        var response = new BaseDTO();
        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage))
        {
            response.ErrorMessage = dto.ErrorMessage;
            return response;
        }

        if (await db.Providers.AnyAsync(x => x.EMail.Equals(dto.EMail)))
        {
            response.ErrorMessage = "Provider with given e-mail already exists.\nPlease wait to be given access.";
            return response;
        }

        var provider = new ProviderModel(long.MinValue, dto.FirstName, dto.MiddleName, dto.LastName, dto.EMail,
            CountryCodeEnum.Select, StateCodeEnum.Select, TimeZoneCodeEnum.Select);

        await ExecuteWithRetryAsync(async () =>
        {
            using var trans = await db.Database.BeginTransactionAsync();
            await db.Providers.AddAsync(provider);
            await db.SaveChangesAsync();

            var emailReq = new UpdateProviderEMailRequestModel(provider.ProviderId, dto.EMail);
            var message =
                $"Thank you for joining AM Tech!\nPlease verify your email:\n{config["Environment:AngularURI"]}/verify-email?guid={emailReq.QueryGuid}&isNew=true";
            var comm = new ProviderCommunicationModel(provider.ProviderId, message, DateTime.MinValue);

            await Task.WhenAll(
                db.UpdateProviderEMailRequests.AddAsync(emailReq).AsTask(),
                db.ProviderCommunications.AddAsync(comm).AsTask()
            );

            await db.SaveChangesAsync();
            await trans.CommitAsync();
        });

        logger.LogAudit($"Provider Id: {provider.ProviderId} - E-Mail: {provider.EMail}");
        return response;
    }

    public async Task<ProviderDTO> GetProviderAsync(string jwt)
    {
        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());
        var provider = new ProviderModel();

        await ExecuteWithRetryAsync(async () =>
        {
            provider = await db.Providers.FirstOrDefaultAsync(u => u.ProviderId == providerId)
                           ?? throw new ArgumentException(nameof(providerId));
            
            if (provider.DeleteDate != null)
            {
                provider.DeleteDate = null;
                db.Providers.Update(provider);
                await db.SaveChangesAsync();
            }
        });

        var dto = new ProviderDTO();
        dto.CreateNewRecordFromModel(provider);
        return dto;
    }

    public async Task<ProviderDTO> UpdateEMailAsync(ProviderDTO dto, string jwt)
    {
        var response = new ProviderDTO();
        var existingProviderExists = false;
        var existingeMailRequestExists = false;
        await ExecuteWithRetryAsync(async () =>
        {
            existingProviderExists = await db.Providers
                .Where(x => x.EMail == dto.EMail)
                .AnyAsync();
            
            existingeMailRequestExists = await db.UpdateProviderEMailRequests
                .Where(x => x.NewEMail == dto.EMail && x.DeleteDate == null)
                .AnyAsync();
        });
        if (!ValidationTool.IsValidEmail(dto.EMail) || existingProviderExists || existingeMailRequestExists)
        {
            response.ErrorMessage = "Provider with given e-mail already exists or e-mail is not in valid format.";
            return response;
        }
            

        var providerId = IdentityTool.GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var request = new UpdateProviderEMailRequestModel(providerId, dto.EMail);
        var message =
            $"There has been a request to change your E-Mail.\n" +
            $"If this was not you, please change your password.\n" +
            $"Otherwise, verify here: {config["Environment:AngularURI"]}/verify-email?guid={request.QueryGuid}&isNew=false\n" +
            $"New E-Mail: {request.NewEMail}";

        var communication = new ProviderCommunicationModel(providerId, message, DateTime.MinValue);

        await ExecuteWithRetryAsync(async () =>
        {
            using var trans = await db.Database.BeginTransactionAsync();

            await db.UpdateProviderEMailRequests
                .Where(x => x.ProviderId == providerId)
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));

            await db.UpdateProviderEMailRequests.AddAsync(request);
            await db.ProviderCommunications.AddAsync(communication);
            await db.SaveChangesAsync();
            await trans.CommitAsync();
        });

        return response;
    }

    public async Task<BaseDTO> UpdateProviderAsync(ProviderDTO dto, string jwt)
    {
        var response = new BaseDTO();
        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage))
        {
            response.ErrorMessage = dto.ErrorMessage;
            return response;
        }

        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var provider = await db.Providers.FirstOrDefaultAsync(x => x.ProviderId == providerId)
                       ?? throw new ArgumentException(nameof(providerId));

        provider.UpdateRecordFromDTO(dto);
        await ExecuteWithRetryAsync(async () =>
        {
            await db.SaveChangesAsync();
        });
        return response;
    }

    public async Task<BaseDTO> VerifyUpdateEMailAsync(string guid)
    {
        var response = new BaseDTO();
        var request = await db.UpdateProviderEMailRequests
            .FirstOrDefaultAsync(x => x.QueryGuid == guid && x.DeleteDate == null);
        if (request == null)
        {
            response.ErrorMessage = "Invalid or expired link.";
            return response;
        }

        var provider = await db.Providers.FirstOrDefaultAsync(x => x.ProviderId == request.ProviderId)
                       ?? throw new InvalidOperationException("Provider not found.");

        var oldEmail = provider.EMail;

        await ExecuteWithRetryAsync(async () =>
        {
            using var trans = await db.Database.BeginTransactionAsync();
            provider.EMail = request.NewEMail;
            provider.EMailVerified = true;
            provider.UpdateDate = DateTime.UtcNow;
            request.DeleteDate = DateTime.UtcNow;

            await db.SaveChangesAsync();
            await trans.CommitAsync();
        });

        logger.LogAudit(
            $"Email Changed - Provider Id: {provider.ProviderId} Old Email: {oldEmail} - New Email: {request.NewEMail}");
        return response;
    }

    // ──────────────────────── Private Methods ────────────────────────
    private async Task ExecuteWithRetryAsync(Func<Task> action, [CallerMemberName] string callerName = "")
    {
        var stopwatch = Stopwatch.StartNew();
        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(2);
        var attempt = 0;

        for (attempt = 1; attempt <= maxRetries; attempt++)
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {

                if (attempt == maxRetries)
                {
                    logger.LogError(ex.ToString());
                    throw;
                }
                await Task.Delay(retryDelay);
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInfo($"{callerName}: {nameof(ExecuteWithRetryAsync)} took {stopwatch.ElapsedMilliseconds} ms with {attempt} attempt(s).");
            }
    }
}
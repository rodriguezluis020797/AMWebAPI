using System.Diagnostics;
using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMServices.CoreServices;

public interface IServiceService
{
    Task<ServiceDTO> CreateServiceAsync(ServiceDTO dto, string jwt);
    Task<List<ServiceDTO>> GetServicesAsync(string jwt);
    Task<ServiceDTO> DeleteServiceAsync(ServiceDTO dto, string jwt);
    Task<ServiceDTO> UpdateServiceAsync(ServiceDTO dto, string jwt);
}

public class ServiceService(IAMLogger logger, AMCoreData db, IConfiguration config) : IServiceService
{
    // ──────────────────────── Create ────────────────────────
    public async Task<ServiceDTO> CreateServiceAsync(ServiceDTO dto, string jwt)
    {
        dto.Validate();
        if (!string.IsNullOrWhiteSpace(dto.ErrorMessage)) return dto;

        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var serviceModel = new ServiceModel(
            providerId,
            dto.Name,
            dto.Description,
            dto.AllowClientScheduling,
            dto.Price
        );
        
        await ExecuteWithRetryAsync(async () =>
            {
                await db.Services.AddAsync(serviceModel);
                await db.SaveChangesAsync();
            });

        return dto;
    }

    // ──────────────────────── Read ────────────────────────
    public async Task<List<ServiceDTO>> GetServicesAsync(string jwt)
    {
        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var services = await db.Services
            .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var serviceList = new List<ServiceDTO>();

        foreach (var model in services)
        {
            var dto = new ServiceDTO();
            dto.AssignFromModel(model);
            CryptographyTool.Encrypt(dto.ServiceId, out var encryptedId);
            dto.ServiceId = encryptedId;
            serviceList.Add(dto);
        }

        return serviceList;
    }

    // ──────────────────────── Update ────────────────────────
    public async Task<ServiceDTO> UpdateServiceAsync(ServiceDTO dto, string jwt)
    {
        dto.Validate();
        if (!string.IsNullOrWhiteSpace(dto.ErrorMessage)) return dto;

        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        CryptographyTool.Decrypt(dto.ServiceId, out var decryptedId);
        var serviceId = long.Parse(decryptedId);

        var serviceModel = await db.Services
            .FirstOrDefaultAsync(x => x.ProviderId == providerId && x.ServiceId == serviceId);

        if (serviceModel == null)
        {
            dto.ErrorMessage = "Service not found.";
            return dto;
        }

        serviceModel.UpdateRecordFromDTO(dto);
        
        await ExecuteWithRetryAsync(async () =>
        {
            db.Update(serviceModel);
            await db.SaveChangesAsync();
        });

        return dto;
    }

    // ──────────────────────── Delete ────────────────────────
    public async Task<ServiceDTO> DeleteServiceAsync(ServiceDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        CryptographyTool.Decrypt(dto.ServiceId, out var decryptedId);
        var serviceId = long.Parse(decryptedId);

        var serviceModel = await db.Services
            .FirstOrDefaultAsync(x => x.ProviderId == providerId && x.ServiceId == serviceId);

        if (serviceModel == null)
        {
            dto.ErrorMessage = "Service not found.";
            return dto;
        }

        serviceModel.DeleteDate = DateTime.UtcNow;
        
        await ExecuteWithRetryAsync(async () =>
        {
            db.Update(serviceModel);
            await db.SaveChangesAsync();
        });

        return dto;
    }
    
    private async Task ExecuteWithRetryAsync(Func<Task> action)
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
                logger.LogInfo($"{nameof(action.Method)}'s {nameof(ExecuteWithRetryAsync)} took {stopwatch.ElapsedMilliseconds} ms with {attempt} attempt(s).");
            }
    }
}
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

public interface IServiceService
{
    Task<ServiceDTO> CreateServiceAsync(ServiceDTO dto, string jwt);
    Task<List<ServiceDTO>> GetServicesAsync(string jwt);
    Task<ServiceDTO> DeleteServiceAsync(ServiceDTO dto, string jwt);
    Task<ServiceDTO> UpdateServiceAsync(ServiceDTO dto, string jwt);
}

public class ServiceService(IAMLogger logger, AMCoreData db, IConfiguration config) : IServiceService
{
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

        var services = new List<ServiceModel>();
        await ExecuteWithRetryAsync(async () =>
        {
            services = await db.Services
                .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
                .OrderBy(x => x.Name)
                .ToListAsync();
        });

        var serviceDTOs = new List<ServiceDTO>();

        foreach (var model in services)
        {
            var dto = new ServiceDTO();
            dto.AssignFromModel(model);
            CryptographyTool.Encrypt(dto.ServiceId, out var encryptedId);
            dto.ServiceId = encryptedId;
            serviceDTOs.Add(dto);
        }

        return serviceDTOs;
    }

    public async Task<ServiceDTO> UpdateServiceAsync(ServiceDTO dto, string jwt)
    {
        dto.Validate();
        if (!string.IsNullOrWhiteSpace(dto.ErrorMessage)) return dto;

        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        CryptographyTool.Decrypt(dto.ServiceId, out var decryptedId);
        var serviceId = long.Parse(decryptedId);

        var serviceModel = new ServiceModel();
        await ExecuteWithRetryAsync(async () =>
        {
            serviceModel = await db.Services
                .Where(x => x.ProviderId == providerId && x.ServiceId == serviceId)
                .FirstOrDefaultAsync();
            
            serviceModel.UpdateRecordFromDTO(dto);
            
            db.Update(serviceModel);
            await db.SaveChangesAsync();
        });

        return dto;
    }

    
    public async Task<ServiceDTO> DeleteServiceAsync(ServiceDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        CryptographyTool.Decrypt(dto.ServiceId, out var decryptedId);
        var serviceId = long.Parse(decryptedId);
        
        var serviceModel = new ServiceModel();
        await ExecuteWithRetryAsync(async () =>
        {
            serviceModel = await db.Services
                .Where(x => x.ProviderId == providerId && x.ServiceId == serviceId)
                .FirstOrDefaultAsync();
            
            serviceModel.DeleteDate = DateTime.UtcNow;
            
            db.Update(serviceModel);
            await db.SaveChangesAsync();
        });

        return dto;
    }

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
                logger.LogInfo(
                    $"{callerName}: {nameof(ExecuteWithRetryAsync)} took {stopwatch.ElapsedMilliseconds} ms with {attempt} attempt(s).");
            }
    }
}
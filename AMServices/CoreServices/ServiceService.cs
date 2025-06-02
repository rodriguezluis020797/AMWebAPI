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
    Task<ServiceDTO> GetServicePrice(ServiceDTO dto, string jwt);
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
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var servicenNameExists = false;
        await db.ExecuteWithRetryAsync(async () =>
        {
            if (await db.Services
                    .Where(x => x.ProviderId == providerId && x.Name.ToLower().Equals(dto.Name.ToLower()) && x.DeleteDate == null)
                    .AnyAsync())
            {
                servicenNameExists = true;
            }
        });

        if (servicenNameExists)
        {
            dto.ErrorMessage = "A service with that name already exists.";
            return dto;
        }

        var serviceModel = new ServiceModel(
            providerId,
            dto.Name,
            dto.Description,
            dto.AllowClientScheduling,
            dto.Price
        );

        await db.ExecuteWithRetryAsync(async () =>
        {
            await db.Services.AddAsync(serviceModel);
            await db.SaveChangesAsync();
        });

        return dto;
    }

    public async Task<List<ServiceDTO>> GetServicesAsync(string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var services = new List<ServiceModel>();
        await db.ExecuteWithRetryAsync(async () =>
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

    public async Task<ServiceDTO> GetServicePrice(ServiceDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        CryptographyTool.Decrypt(dto.ServiceId, out var decryptedId);
        var serviceId = long.Parse(decryptedId);

        await db.ExecuteWithRetryAsync(async () =>
        {
            dto.Price = await db.Services
                .Where(x => x.ProviderId == providerId && x.ServiceId == serviceId)
                .Select(x => x.Price)
                .FirstOrDefaultAsync();
        });

        return dto;
    }

    public async Task<ServiceDTO> UpdateServiceAsync(ServiceDTO dto, string jwt)
    {
        dto.Validate();
        if (!string.IsNullOrWhiteSpace(dto.ErrorMessage)) return dto;

        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        CryptographyTool.Decrypt(dto.ServiceId, out var decryptedId);
        var serviceId = long.Parse(decryptedId);

        var serviceModel = new ServiceModel();
        await db.ExecuteWithRetryAsync(async () =>
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
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        CryptographyTool.Decrypt(dto.ServiceId, out var decryptedId);
        var serviceId = long.Parse(decryptedId);
        var appoitnmentExists = false;
        
        await db.ExecuteWithRetryAsync(async () =>
        {
            appoitnmentExists = await db.Appointments
                .Where(x => x.ServiceId == serviceId && x.DeleteDate == null && x.Status != AppointmentStatusEnum.Scheduled)
                .AnyAsync();

            if (appoitnmentExists)
            {
                return;
            }

            await db.Services
                .Where(x => x.ProviderId == providerId && x.ServiceId == serviceId)
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));

            await db.SaveChangesAsync();
        });

        if (appoitnmentExists)
        {
            dto.ErrorMessage = "This service cannot be deleted because it is currently in use.";
        }

        return dto;
    }
}
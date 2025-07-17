using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMServices.DataServices;
using AMTools;
using MCCDotnetTools;
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

public class ServiceService(AMCoreData db, IConfiguration config) : IServiceService
{
    public async Task<ServiceDTO> CreateServiceAsync(ServiceDTO dto, string jwt)
    {
        dto.Validate();
        if (!string.IsNullOrWhiteSpace(dto.ErrorMessage)) return dto;

        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        var serviceNameExists = false;
        await db.ExecuteWithRetryAsync(async () =>
        {
            if (await db.Services
                    .Where(x => x.ProviderId == providerId && x.Name.ToLower().Equals(dto.Name.ToLower()) &&
                                x.DeleteDate == null)
                    .AnyAsync())
                serviceNameExists = true;
        });

        if (serviceNameExists)
        {
            dto.ErrorMessage = "A service with that name already exists.";
            return dto;
        }

        var serviceModel = new ServiceModel(
            providerId,
            dto.Name,
            dto.Description ?? null,
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
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

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
            MCCCryptographyTool.Encrypt(dto.ServiceId, out var encryptedId, config["Cryptography:Key"]!, config["Cryptography:IV"]!);
            dto.ServiceId = encryptedId;
            serviceDTOs.Add(dto);
        }

        return serviceDTOs;
    }

    public async Task<ServiceDTO> GetServicePrice(ServiceDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        MCCCryptographyTool.Decrypt(dto.ServiceId, out var decryptedId, config["Cryptography:Key"]!, config["Cryptography:IV"]!);
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
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        MCCCryptographyTool.Decrypt(dto.ServiceId, out var decryptedId, config["Cryptography:Key"]!, config["Cryptography:IV"]!);
        var serviceId = long.Parse(decryptedId);

        await db.ExecuteWithRetryAsync(async () =>
        {
            var serviceModel = await db.Services
                .Where(x => x.ProviderId == providerId && x.ServiceId == serviceId)
                .FirstOrDefaultAsync();

            if (serviceModel == null) throw new Exception("Service not found");

            serviceModel.UpdateRecordFromDTO(dto);

            db.Update(serviceModel);
            await db.SaveChangesAsync();
        });

        return dto;
    }


    public async Task<ServiceDTO> DeleteServiceAsync(ServiceDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        MCCCryptographyTool.Decrypt(dto.ServiceId, out var decryptedId, config["Cryptography:Key"]!, config["Cryptography:IV"]!);
        var serviceId = long.Parse(decryptedId);
        var appointmentExists = false;

        await db.ExecuteWithRetryAsync(async () =>
        {
            appointmentExists = await db.Appointments
                .Where(x => x.ServiceId == serviceId && x.DeleteDate == null &&
                            x.Status != AppointmentStatusEnum.Scheduled)
                .AnyAsync();

            if (appointmentExists) return;

            await db.Services
                .Where(x => x.ProviderId == providerId && x.ServiceId == serviceId)
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));

            await db.SaveChangesAsync();
        });

        if (appointmentExists) dto.ErrorMessage = "This service cannot be deleted because it is currently in use.";

        return dto;
    }
}
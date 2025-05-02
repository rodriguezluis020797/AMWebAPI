using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMWebAPI.Services.CoreServices;

public interface IServiceService
{
    Task<ServiceDTO> CreateServiceAsync(ServiceDTO dto, string jwt);
    Task<List<ServiceDTO>> GetServicesAsync(string jwt);
    Task<ServiceDTO> DeleteServiceAsync(ServiceDTO dto, string jwt);
    Task<ServiceDTO> UpdateServiceAsync(ServiceDTO dt, string jwt);
}

public class ServiceService : IServiceService
{
    private readonly IConfiguration _config;
    private readonly AMCoreData _db;
    private readonly IAMLogger _logger;

    public ServiceService(IAMLogger logger, AMCoreData db, IConfiguration config)
    {
        _logger = logger;
        _db = db;
        _config = config;
    }

    public async Task<ServiceDTO> CreateServiceAsync(ServiceDTO dto, string jwt)
    {
        var response = new BaseDTO();

        dto.Validate();
        if (!string.IsNullOrWhiteSpace(dto.ErrorMessage))
        {
            response.ErrorMessage = dto.ErrorMessage;
            return dto;
        }

        var providerId = GetProviderIdFromJwt(jwt);

        var serviceModel =
            new ServiceModel(providerId, dto.Name, dto.Description, dto.AllowClientScheduling, dto.Price);

        await _db.Services.AddAsync(serviceModel);
        await _db.SaveChangesAsync();

        return dto;
    }

    public async Task<List<ServiceDTO>> GetServicesAsync(string jwt)
    {
        var providerId = GetProviderIdFromJwt(jwt);

        var serviceModels = await _db.Services
            .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var serviceList = new List<ServiceDTO>();
        var serviceDto = new ServiceDTO();

        foreach (var serviceModel in serviceModels)
        {
            serviceDto.AssignFromModel(serviceModel);
            CryptographyTool.Encrypt(serviceDto.ServiceId, out var encryptedText);
            serviceDto.ServiceId = encryptedText;
            serviceList.Add(serviceDto);
            serviceDto = new ServiceDTO();
        }

        return serviceList;
    }

    public async Task<ServiceDTO> DeleteServiceAsync(ServiceDTO dto, string jwt)
    {
        var response = new ServiceDTO();
        var providerId = GetProviderIdFromJwt(jwt);
        CryptographyTool.Decrypt(dto.ServiceId, out var decryptedText);
        var serviceId = long.Parse(decryptedText);

        var serviceModel = await _db.Services
            .Where(x => x.ProviderId == providerId && x.ServiceId == serviceId)
            .FirstOrDefaultAsync();

        serviceModel.DeleteDate = DateTime.UtcNow;

        _db.Update(serviceModel);
        await _db.SaveChangesAsync();

        return response;
    }

    public async Task<ServiceDTO> UpdateServiceAsync(ServiceDTO dto, string jwt)
    {
        var response = new ServiceDTO();
        dto.Validate();
        if (!string.IsNullOrWhiteSpace(dto.ErrorMessage))
        {
            response.ErrorMessage = dto.ErrorMessage;
            return dto;
        }

        var providerId = GetProviderIdFromJwt(jwt);
        CryptographyTool.Decrypt(dto.ServiceId, out var decryptedText);
        var serviceId = long.Parse(decryptedText);

        var serviceModel = await _db.Services
            .Where(x => x.ProviderId == providerId && x.ServiceId == serviceId)
            .FirstOrDefaultAsync();

        _db.Update(serviceModel);
        await _db.SaveChangesAsync();

        return response;
    }

    private long GetProviderIdFromJwt(string jwToken)
    {
        var claims = IdentityTool.GetClaimsFromJwt(jwToken, _config["Jwt:Key"]!);
        if (!long.TryParse(claims.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value, out var providerId))
            throw new ArgumentException("Invalid provider ID in JWT.");

        return providerId;
    }
}
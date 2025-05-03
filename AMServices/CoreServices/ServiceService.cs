using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMServices.CoreServices
{
    public interface IServiceService
    {
        Task<ServiceDTO> CreateServiceAsync(ServiceDTO dto, string jwt);
        Task<List<ServiceDTO>> GetServicesAsync(string jwt);
        Task<ServiceDTO> DeleteServiceAsync(ServiceDTO dto, string jwt);
        Task<ServiceDTO> UpdateServiceAsync(ServiceDTO dto, string jwt);
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

        // ──────────────────────── Create ────────────────────────
        public async Task<ServiceDTO> CreateServiceAsync(ServiceDTO dto, string jwt)
        {
            dto.Validate();
            if (!string.IsNullOrWhiteSpace(dto.ErrorMessage))
            {
                return dto;
            }

            var providerId = GetProviderIdFromJwt(jwt);

            var serviceModel = new ServiceModel(
                providerId,
                dto.Name,
                dto.Description,
                dto.AllowClientScheduling,
                dto.Price
            );

            await _db.Services.AddAsync(serviceModel);
            await _db.SaveChangesAsync();

            return dto;
        }

        // ──────────────────────── Read ────────────────────────
        public async Task<List<ServiceDTO>> GetServicesAsync(string jwt)
        {
            var providerId = GetProviderIdFromJwt(jwt);

            var services = await _db.Services
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
            if (!string.IsNullOrWhiteSpace(dto.ErrorMessage))
            {
                return dto;
            }

            var providerId = GetProviderIdFromJwt(jwt);
            CryptographyTool.Decrypt(dto.ServiceId, out var decryptedId);
            var serviceId = long.Parse(decryptedId);

            var serviceModel = await _db.Services
                .FirstOrDefaultAsync(x => x.ProviderId == providerId && x.ServiceId == serviceId);

            if (serviceModel == null)
            {
                dto.ErrorMessage = "Service not found.";
                return dto;
            }

            serviceModel.UpdateRecordFromDTO(dto);

            _db.Update(serviceModel);
            await _db.SaveChangesAsync();

            return dto;
        }

        // ──────────────────────── Delete ────────────────────────
        public async Task<ServiceDTO> DeleteServiceAsync(ServiceDTO dto, string jwt)
        {
            var providerId = GetProviderIdFromJwt(jwt);
            CryptographyTool.Decrypt(dto.ServiceId, out var decryptedId);
            var serviceId = long.Parse(decryptedId);

            var serviceModel = await _db.Services
                .FirstOrDefaultAsync(x => x.ProviderId == providerId && x.ServiceId == serviceId);

            if (serviceModel == null)
            {
                dto.ErrorMessage = "Service not found.";
                return dto;
            }

            serviceModel.DeleteDate = DateTime.UtcNow;

            _db.Update(serviceModel);
            await _db.SaveChangesAsync();

            return dto;
        }

        // ──────────────────────── Helpers ────────────────────────
        private long GetProviderIdFromJwt(string jwToken)
        {
            var claims = IdentityTool.GetClaimsFromJwt(jwToken, _config["Jwt:Key"]!);
            if (!long.TryParse(claims.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value, out var providerId))
                throw new ArgumentException("Invalid provider ID in JWT.");

            return providerId;
        }
    }
}
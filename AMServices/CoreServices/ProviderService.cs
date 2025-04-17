using AMData.Models;
using AMData.Models.CoreModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.DataServices;
using AMWebAPI.Services.IdentityServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMWebAPI.Services.CoreServices
{
    public interface IProviderService
    {
        Task<ProviderDTO> CreateProvider(ProviderDTO dto);
        Task<ProviderDTO> GetProvider(string jwToken);
    }

    public class ProviderService : IProviderService
    {
        private readonly IAMLogger _logger;
        private readonly AMCoreData _db;
        private readonly ICommunicationService _communicationService;
        private readonly IConfiguration _config;

        public ProviderService(
            IAMLogger logger,
            AMCoreData db,
            IIdentityService identityService, // NOTE: If unused, consider removing
            ICommunicationService communicationService,
            IConfiguration config)
        {
            _logger = logger;
            _db = db;
            _communicationService = communicationService;
            _config = config;
        }

        public async Task<ProviderDTO> CreateProvider(ProviderDTO dto)
        {
            dto.Validate();
            if (!string.IsNullOrEmpty(dto.ErrorMessage))
                return dto;

            bool providerExists = _db.Providers.Any(x => x.EMail == dto.EMail);
            if (providerExists)
            {
                dto.ErrorMessage = $"Provider with given e-mail already exists.{Environment.NewLine}" +
                                   $"Please wait to be given access.";
                return dto;
            }

            var provider = new ProviderModel();
            provider.CreateNewRecordFromDTO(dto);

            await _db.Providers.AddAsync(provider);
            await _db.SaveChangesAsync();

            await TrySendNewProviderMessage(provider.ProviderId);

            _logger.LogAudit($"Provider Id: {provider.ProviderId}{Environment.NewLine}E-Mail: {provider.EMail}");

            dto.CreateNewRecordFromModel(provider);
            return dto;
        }

        public async Task<ProviderDTO> GetProvider(string jwToken)
        {
            var claims = IdentityTool.GetClaimsFromJwt(jwToken, _config["Jwt:Key"]!);
            var providerId = Convert.ToInt64(claims.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value);

            var provider = await _db.Providers.FirstOrDefaultAsync(u => u.ProviderId == providerId);
            if (provider == null)
                throw new ArgumentException(nameof(providerId));

            var dto = new ProviderDTO();
            dto.CreateNewRecordFromModel(provider);
            return dto;
        }

        private async Task TrySendNewProviderMessage(long providerId)
        {
            var message = _config["Messages:NewProviderMessage"];
            if (string.IsNullOrWhiteSpace(message))
                return;

            try
            {
                await _communicationService.AddProviderCommunication(providerId, message);
            }
            catch
            {
                // Silent fail — UI will display message anyway
            }
        }
    }
}
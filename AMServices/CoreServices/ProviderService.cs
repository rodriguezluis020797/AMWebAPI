using AMData.Models;
using AMData.Models.CoreModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.DataServices;
using AMWebAPI.Services.IdentityServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Transactions;

namespace AMWebAPI.Services.CoreServices
{
    public interface IProviderService
    {
        Task<ProviderDTO> CreateProviderAsync(ProviderDTO dto);
        Task<ProviderDTO> UpdateProviderAsync(ProviderDTO dto, string jwToken);
        Task<ProviderDTO> UpdateEMailAsync(ProviderDTO dto, string jwToken);
        Task<ProviderDTO> GetProviderAsync(string jwToken);
    }

    public class ProviderService : IProviderService
    {
        private readonly IAMLogger _logger;
        private readonly AMCoreData _db;
        private readonly IConfiguration _config;

        public ProviderService(
            IAMLogger logger,
            AMCoreData db,
            IConfiguration config)
        {
            _logger = logger;
            _db = db;
            _config = config;
        }

        public async Task<ProviderDTO> CreateProviderAsync(ProviderDTO dto)
        {
            dto.Validate();
            if (!string.IsNullOrEmpty(dto.ErrorMessage))
                return dto;

            if (await _db.Providers.AnyAsync(x => x.EMail == dto.EMail))
            {
                dto.ErrorMessage = $"Provider with given e-mail already exists.{Environment.NewLine}Please wait to be given access.";
                return dto;
            }

            var provider = new ProviderModel();
            provider.CreateNewRecordFromDTO(dto);


            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            await _db.Providers.AddAsync(provider);
            await _db.SaveChangesAsync();

            var providerComm = new ProviderCommunicationModel
            {
                CreateDate = DateTime.UtcNow,
                ProviderId = provider.ProviderId,
                Message = _config["Messages:NewProviderMessage"]
            };
            await _db.ProviderCommunications.AddAsync(providerComm);
            await _db.SaveChangesAsync();
            scope.Complete();

            _logger.LogAudit($"Provider Id: {provider.ProviderId}{Environment.NewLine}E-Mail: {provider.EMail}");

            dto.CreateNewRecordFromModel(provider);
            return dto;
        }

        public async Task<ProviderDTO> GetProviderAsync(string jwToken)
        {
            var providerId = GetProviderIdFromJwt(jwToken);

            var provider = await _db.Providers
                .FirstOrDefaultAsync(u => u.ProviderId == providerId)
                ?? throw new ArgumentException(nameof(providerId));

            var dto = new ProviderDTO();
            dto.CreateNewRecordFromModel(provider);
            return dto;
        }

        public async Task<bool> UpdateEMailAsync(ProviderDTO dto, string jwToken)
        {
            var principal = IdentityTool.GetClaimsFromJwt(jwToken, _config["Jwt:Key"]!);
            var providerId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value);
            var sessionId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.SessionId.ToString())?.Value);

            var provider = await _db.Providers
                .Where(x => x.ProviderId == providerId)
                .FirstOrDefaultAsync();

            var request = new UpdateProviderEMailRequestModel()
            {
                CreateDate = DateTime.UtcNow,
                DeleteDate = null,
                NewEMail = dto.EMail,
                ProviderId = providerId,
            };

            var communication = new ProviderCommunicationModel()
            {
                AttemptOne = null,
                AttemptThree = null,
                AttemptTwo = null,
                CreateDate = DateTime.UtcNow,
                DeleteDate = null,
                Message = $"There has been a request to change your E-Mail.{Environment.NewLine}" +
                "If this was not you, please change your password as soon as possible."
            };

            return true;
        }

        public async Task<ProviderDTO> UpdateProviderAsync(ProviderDTO dto, string jwToken)
        {
            dto.Validate();
            if (!string.IsNullOrEmpty(dto.ErrorMessage))
                return dto;

            var providerId = GetProviderIdFromJwt(jwToken);

            var provider = await _db.Providers
                .FirstOrDefaultAsync(x => x.ProviderId == providerId)
                ?? throw new ArgumentException(nameof(providerId));

            provider.UpdateRecordFromDTO(dto);
            await _db.SaveChangesAsync();

            return dto;
        }

        private long GetProviderIdFromJwt(string jwToken)
        {
            var claims = IdentityTool.GetClaimsFromJwt(jwToken, _config["Jwt:Key"]!);
            if (!long.TryParse(claims.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value, out var providerId))
                throw new ArgumentException("Invalid provider ID in JWT.");

            return providerId;
        }

        Task<ProviderDTO> IProviderService.UpdateEMailAsync(ProviderDTO dto, string jwToken)
        {
            throw new NotImplementedException();
        }
    }
}
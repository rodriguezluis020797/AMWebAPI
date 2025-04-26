using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMData.Models.IdentityModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data.SqlTypes;
using System.Transactions;

namespace AMWebAPI.Services.CoreServices
{
    public interface IProviderService
    {
        Task<BaseDTO> CreateProviderAsync(ProviderDTO dto);
        Task<ProviderDTO> GetProviderAsync(string jwToken);
        Task<ProviderDTO> UpdateEMailAsync(ProviderDTO dto, string jwToken);
        Task<ProviderDTO> UpdateProviderAsync(ProviderDTO dto, string jwToken);
        Task<BaseDTO> VerifyUpdateEMailAsync(string guid);
    }

    public class ProviderService : IProviderService
    {
        // ──────────────────────── Private Fields ────────────────────────

        private readonly IAMLogger _logger;
        private readonly AMCoreData _db;
        private readonly IConfiguration _config;

        // ──────────────────────── Constructor ────────────────────────

        public ProviderService(
            IAMLogger logger,
            AMCoreData db,
            IConfiguration config)
        {
            _logger = logger;
            _db = db;
            _config = config;
        }

        // ──────────────────────── Public Methods ────────────────────────

        public async Task<BaseDTO> CreateProviderAsync(ProviderDTO dto)
        {
            var response = new BaseDTO();

            dto.Validate();
            if (!string.IsNullOrEmpty(dto.ErrorMessage))
            {
                response.ErrorMessage = dto.ErrorMessage;
                return response;
            }

            if (await _db.Providers.AnyAsync(x => x.EMail == dto.EMail))
            {
                response.ErrorMessage = $"Provider with given e-mail already exists.\nPlease wait to be given access.";
                return response;
            }

            var provider = new ProviderModel(0, dto.FirstName, dto.MiddleName, dto.LastName, dto.EMail);

            var maxRetries = 3;
            var retryDelay = TimeSpan.FromSeconds(2);
            var attempt = 0;

            while (true)
            {
                try
                {
                    using (var trans = await _db.Database.BeginTransactionAsync())
                    {
                        await _db.Providers.AddAsync(provider);
                        await _db.SaveChangesAsync();

                        var emailReq = new UpdateProviderEMailRequestModel(provider.ProviderId, dto.EMail);
                        var message = $"Thank you for joining AM Tech!\nPlease verify your email by clicking the following link:\n{_config["Environment:AngularURI"]}/verify-email?guid={emailReq.QueryGuid}&isNew=true";

                        var comm = new ProviderCommunicationModel(provider.ProviderId, message, DateTime.MinValue);

                        var addReqTask = _db.UpdateProviderEMailRequests.AddAsync(emailReq).AsTask();
                        var addCommTask = _db.ProviderCommunications.AddAsync(comm).AsTask();

                        await Task.WhenAll(addReqTask, addCommTask);
                        await _db.SaveChangesAsync();
                        await trans.CommitAsync();
                    }

                    _logger.LogInfo("All database changes completed successfully.");
                    break;
                }
                catch (Exception ex)
                {
                    attempt++;
                    _logger.LogError($"Attempt {attempt} failed: {ex.Message}");

                    if (attempt >= maxRetries)
                    {
                        _logger.LogError("All attempts failed. No data was committed.");
                        throw;
                    }

                    await Task.Delay(retryDelay);
                }
            }

            _logger.LogAudit($"Provider Id: {provider.ProviderId} - E-Mail: {provider.EMail}");

            return response;
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

        public async Task<ProviderDTO> UpdateEMailAsync(ProviderDTO dto, string jwToken)
        {
            if (await _db.Providers.AnyAsync(x => x.EMail == dto.EMail) ||
                await _db.UpdateProviderEMailRequests.AnyAsync(x => x.NewEMail.Equals(dto.EMail) && x.DeleteDate == null) ||
                !ValidationTool.IsValidEmail(dto.EMail))
            {
                return new ProviderDTO
                {
                    ErrorMessage = $"Provider with given e-mail already exists or e-mail is not in valid format."
                };
            }

            var principal = IdentityTool.GetClaimsFromJwt(jwToken, _config["Jwt:Key"]!);
            var providerId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value);
            var sessionId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.SessionId.ToString())?.Value);

            var provider = await _db.Providers.FirstOrDefaultAsync(x => x.ProviderId == providerId);

            var request = new UpdateProviderEMailRequestModel(providerId, dto.EMail);
            var message = $"There has been a request to change your E-Mail.{Environment.NewLine}If this was not you, please change your password as soon as possible.{Environment.NewLine}Otherwise, click the link below to approve the new E-Mail address.Link: {_config["Environement:AngularURI"]}/verify-email?guid={request.QueryGuid}&isNew=falseNew E-Mail Address: {request.NewEMail}";

            var communication = new ProviderCommunicationModel(providerId, message, DateTime.MinValue);

            var maxRetries = 3;
            var retryDelay = TimeSpan.FromSeconds(2);
            var attempt = 0;

            while (true)
            {
                try
                {
                    using (var transaction = await _db.Database.BeginTransactionAsync())
                    {
                        await _db.UpdateProviderEMailRequests
                        .Where(x => x.ProviderId == providerId)
                        .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));

                        await _db.UpdateProviderEMailRequests.AddAsync(request);
                        await _db.ProviderCommunications.AddAsync(communication);
                        await _db.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }

                    _logger.LogInfo("All database changes completed successfully.");
                    break;
                }
                catch (Exception ex)
                {
                    attempt++;
                    _logger.LogError($"Attempt {attempt} failed: {ex.Message}");

                    if (attempt >= maxRetries)
                    {
                        _logger.LogError("All attempts failed. No data was committed.");
                        throw;
                    }

                    await Task.Delay(retryDelay);
                }
            }

            return dto;
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

        public async Task<BaseDTO> VerifyUpdateEMailAsync(string guid)
        {
            var response = new BaseDTO();
            var request = await _db.UpdateProviderEMailRequests
                .Where(x => x.QueryGuid.Equals(guid) && x.DeleteDate == null)
                .FirstOrDefaultAsync();

            var provider = await _db.Providers
                .Where(x => x.ProviderId == request.ProviderId)
                .FirstOrDefaultAsync();

            var oldEmail = provider.EMail;

            var maxRetries = 3;
            var retryDelay = TimeSpan.FromSeconds(2);
            var attempt = 0;

            while (true)
            {
                try
                {
                    using (var trans = await _db.Database.BeginTransactionAsync())
                    {
                        provider.EMail = request.NewEMail;
                        provider.EMailVerified = true;
                        provider.UpdateDate = DateTime.UtcNow;
                        request.DeleteDate = DateTime.UtcNow;

                        _db.SaveChanges();
                        await trans.CommitAsync();
                    }

                    _logger.LogInfo("All database changes completed successfully.");
                    break;
                }
                catch (Exception ex)
                {
                    attempt++;
                    _logger.LogError($"Attempt {attempt} failed: {ex.Message}");

                    if (attempt >= maxRetries)
                    {
                        _logger.LogError("All attempts failed. No data was committed.");
                        throw;
                    }

                    await Task.Delay(retryDelay);
                }
            }

            _logger.LogAudit($"Email Changed - Provider Id: {provider.ProviderId} Old Email: {oldEmail} - New Email: {request.NewEMail}");
            return response;
        }

        // ──────────────────────── Private Methods ────────────────────────

        private long GetProviderIdFromJwt(string jwToken)
        {
            var claims = IdentityTool.GetClaimsFromJwt(jwToken, _config["Jwt:Key"]!);
            if (!long.TryParse(claims.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value, out var providerId))
                throw new ArgumentException("Invalid provider ID in JWT.");

            return providerId;
        }
    }
}
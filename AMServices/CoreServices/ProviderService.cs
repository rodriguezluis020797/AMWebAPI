using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMWebAPI.Services.CoreServices
{
    public interface IProviderService
    {
        Task<BaseDTO> CreateProviderAsync(ProviderDTO dto);
        Task<ProviderDTO> GetProviderAsync(string jwToken);
        Task<List<StateCodeEnum>> GetStateCodes(CountryCodeEnum countryCode);
        Task<ProviderDTO> UpdateEMailAsync(ProviderDTO dto, string jwToken);
        Task<BaseDTO> UpdateProviderAsync(ProviderDTO dto, string jwToken);
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

            var provider = new ProviderModel(long.MinValue, dto.FirstName, dto.MiddleName, dto.LastName, dto.EMail, CountryCodeEnum.Select, StateCodeEnum.Select, TimeZoneCodeEnum.Select);

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

        public async Task<List<StateCodeEnum>> GetStateCodes(CountryCodeEnum countryCode)
        {
            var states = new List<StateCodeEnum>();
            states.Add(StateCodeEnum.Select);

            switch (countryCode)
            {
                case CountryCodeEnum.United_States:
                    states.Add(StateCodeEnum.Alabama);
                    states.Add(StateCodeEnum.Alaska);
                    states.Add(StateCodeEnum.Arizona);
                    states.Add(StateCodeEnum.Arkansas);
                    states.Add(StateCodeEnum.California);
                    states.Add(StateCodeEnum.Colorado);
                    states.Add(StateCodeEnum.Connecticut);
                    states.Add(StateCodeEnum.Delaware);
                    states.Add(StateCodeEnum.District_of_Columbia);
                    states.Add(StateCodeEnum.Florida);
                    states.Add(StateCodeEnum.Georgia);
                    states.Add(StateCodeEnum.Hawaii);
                    states.Add(StateCodeEnum.Idaho);
                    states.Add(StateCodeEnum.Illinois);
                    states.Add(StateCodeEnum.Indiana);
                    states.Add(StateCodeEnum.Iowa);
                    states.Add(StateCodeEnum.Kansas);
                    states.Add(StateCodeEnum.Kentucky);
                    states.Add(StateCodeEnum.Louisiana);
                    states.Add(StateCodeEnum.Maine);
                    states.Add(StateCodeEnum.Maryland);
                    states.Add(StateCodeEnum.Massachusetts);
                    states.Add(StateCodeEnum.Michigan);
                    states.Add(StateCodeEnum.Minnesota);
                    states.Add(StateCodeEnum.Mississippi);
                    states.Add(StateCodeEnum.Missouri);
                    states.Add(StateCodeEnum.Montana);
                    states.Add(StateCodeEnum.Nebraska);
                    states.Add(StateCodeEnum.Nevada);
                    states.Add(StateCodeEnum.New_Hampshire);
                    states.Add(StateCodeEnum.New_Jersey);
                    states.Add(StateCodeEnum.New_Mexico);
                    states.Add(StateCodeEnum.New_York);
                    states.Add(StateCodeEnum.North_Carolina);
                    states.Add(StateCodeEnum.North_Dakota);
                    states.Add(StateCodeEnum.Ohio);
                    states.Add(StateCodeEnum.Oklahoma);
                    states.Add(StateCodeEnum.Oregon);
                    states.Add(StateCodeEnum.Pennsylvania);
                    states.Add(StateCodeEnum.Rhode_Island);
                    states.Add(StateCodeEnum.South_Carolina);
                    states.Add(StateCodeEnum.South_Dakota);
                    states.Add(StateCodeEnum.Tennessee);
                    states.Add(StateCodeEnum.Texas);
                    states.Add(StateCodeEnum.Utah);
                    states.Add(StateCodeEnum.Vermont);
                    states.Add(StateCodeEnum.Virginia);
                    states.Add(StateCodeEnum.Washington);
                    states.Add(StateCodeEnum.West_Virginia);
                    states.Add(StateCodeEnum.Wisconsin);
                    states.Add(StateCodeEnum.Wyoming);
                    break;

                case CountryCodeEnum.Mexico:
                    states.Add(StateCodeEnum.Aguascalientes);
                    states.Add(StateCodeEnum.Baja_California);
                    states.Add(StateCodeEnum.Baja_California_Sur);
                    states.Add(StateCodeEnum.Campeche);
                    states.Add(StateCodeEnum.Chiapas);
                    states.Add(StateCodeEnum.Chihuahua);
                    states.Add(StateCodeEnum.Ciudad_de_México);
                    states.Add(StateCodeEnum.Coahuila);
                    states.Add(StateCodeEnum.Colima);
                    states.Add(StateCodeEnum.Durango);
                    states.Add(StateCodeEnum.Guanajuato);
                    states.Add(StateCodeEnum.Guerrero);
                    states.Add(StateCodeEnum.Hidalgo);
                    states.Add(StateCodeEnum.Jalisco);
                    states.Add(StateCodeEnum.México);
                    states.Add(StateCodeEnum.Michoacán);
                    states.Add(StateCodeEnum.Morelos);
                    states.Add(StateCodeEnum.Nayarit);
                    states.Add(StateCodeEnum.Nuevo_León);
                    states.Add(StateCodeEnum.Oaxaca);
                    states.Add(StateCodeEnum.Puebla);
                    states.Add(StateCodeEnum.Querétaro);
                    states.Add(StateCodeEnum.Quintana_Roo);
                    states.Add(StateCodeEnum.San_Luis_Potosí);
                    states.Add(StateCodeEnum.Sinaloa);
                    states.Add(StateCodeEnum.Sonora);
                    states.Add(StateCodeEnum.Tabasco);
                    states.Add(StateCodeEnum.Tamaulipas);
                    states.Add(StateCodeEnum.Tlaxcala);
                    states.Add(StateCodeEnum.Veracruz);
                    states.Add(StateCodeEnum.Yucatán);
                    states.Add(StateCodeEnum.Zacatecas);
                    break;

                default:
                    throw new ArgumentException(nameof(CountryCodeEnum));
            }


            return states;
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

        public async Task<BaseDTO> UpdateProviderAsync(ProviderDTO dto, string jwToken)
        {
            var response = new BaseDTO();
            dto.Validate();
            if (!string.IsNullOrEmpty(dto.ErrorMessage))
            {
                response.ErrorMessage = dto.ErrorMessage;
                return response;
            }

            var providerId = GetProviderIdFromJwt(jwToken);

            var provider = await _db.Providers
                .FirstOrDefaultAsync(x => x.ProviderId == providerId)
                ?? throw new ArgumentException(nameof(providerId));

            provider.UpdateRecordFromDTO(dto);
            await _db.SaveChangesAsync();

            return response;
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
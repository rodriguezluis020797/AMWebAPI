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
    public interface IClientService
    {
        Task<ClientDTO> CreateClient(ClientDTO client, string jwt);
    }
    public class ClientService : IClientService
    {
        private readonly IAMLogger _logger;
        private readonly AMCoreData _db;
        private readonly IConfiguration _config;
        
        public ClientService(
            IAMLogger logger,
            AMCoreData db,
            IConfiguration config)
        {
            _logger = logger;
            _db = db;
            _config = config;
        }
        
        public async Task<ClientDTO> CreateClient(ClientDTO dto, string jwt)
        {
            var response = new ClientDTO();

            dto.Validate();

            if (!string.IsNullOrEmpty(dto.ErrorMessage))
            {
                response.ErrorMessage = dto.ErrorMessage;
                return response;
            }
            
            if (await _db.Clients.AnyAsync(x => x.PhoneNumber == dto.PhoneNumber))
            {
                response.ErrorMessage = "A client with the given phone number already exists.";
                return response;
            }

            if (await _db.Clients.AnyAsync(x =>
                    x.FirstName == dto.FirstName &&
                    x.MiddleName == dto.MiddleName &&
                    x.LastName == dto.LastName))
            {
                response.ErrorMessage = "A client with the same name already exists.";
                return response;
            }
            var principal = IdentityTool.GetClaimsFromJwt(jwt, _config["Jwt:Key"]!);
            var providerId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value);
            var sessionId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.SessionId.ToString())?.Value);

            var clientModel = new ClientModel(providerId, dto.FirstName, dto.MiddleName, dto.LastName, dto.PhoneNumber);
            
            var maxRetries = 3;
            var retryDelay = TimeSpan.FromSeconds(2);
            var attempt = 0;

            while (true)
            {
                try
                {
                    using (var trans = await _db.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            await _db.Clients.AddAsync(clientModel);
                            // await _db.ClientCommunication.AddAsync(clientCommunicationModel);
                            await _db.SaveChangesAsync();
                            await trans.CommitAsync();
                        }
                        catch
                        {
                            /*
                             * TODO:
                             * -Add this to other transactions.
                             */
                            await trans.RollbackAsync();
                            throw new Exception();
                        }
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
            
            return response;
        }
    }   
}
using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMServices.CoreServices;

public interface IClientService
{
    Task<ClientDTO> CreateClient(ClientDTO client, string jwt);
    Task<ClientDTO> DeleteClient(ClientDTO dto, string jwt);
    Task<List<ClientDTO>> GetClients(string jwt);
    Task<ClientDTO> UpdateClient(ClientDTO client, string jwt);
}

public class ClientService : IClientService
{
    private readonly IConfiguration _config;
    private readonly AMCoreData _db;
    private readonly IAMLogger _logger;

    public ClientService(IAMLogger logger, AMCoreData db, IConfiguration config)
    {
        _logger = logger;
        _db = db;
        _config = config;
    }

    public async Task<ClientDTO> CreateClient(ClientDTO dto, string jwt)
    {
        var response = new ClientDTO();

        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage)) return dto;

        if (await ClientExists(dto, false))
        {
            response.ErrorMessage = "A client with the given phone number or name already exists.";
            return response;
        }

        var providerId = IdentityTool
            .GetJwtClaimById(jwt, _config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());
        
        var clientModel = new ClientModel(providerId, dto.FirstName, dto.MiddleName, dto.LastName, dto.PhoneNumber);

        await ExecuteWithRetry(async () =>
        {
            await _db.Clients.AddAsync(clientModel);
            await _db.SaveChangesAsync();
        });

        return response;
    }

    public async Task<ClientDTO> UpdateClient(ClientDTO dto, string jwt)
    {
        var response = new ClientDTO();

        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage)) return dto;

        if (await ClientExists(dto, true))
        {
            response.ErrorMessage = "A client with the given phone number or name already exists.";
            return response;
        }

        var providerId = IdentityTool
            .GetJwtClaimById(jwt, _config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());
        
        CryptographyTool.Decrypt(dto.ClientId, out var decryptedId);

        var clientModel = await _db.Clients.FirstOrDefaultAsync(x => x.ClientId == long.Parse(decryptedId) && x.ProviderId == providerId);
        clientModel.UpdateRecordFromDTO(dto);

        await ExecuteWithRetry(async () =>
        {
            _db.Clients.Update(clientModel);
            await _db.SaveChangesAsync();
        });

        return response;
    }

    public async Task<ClientDTO> DeleteClient(ClientDTO dto, string jwt)
    {
        var response = new ClientDTO();

        var providerId = IdentityTool
            .GetJwtClaimById(jwt, _config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());
        
        CryptographyTool.Decrypt(dto.ClientId, out var decryptedId);

        var clientModel = await _db.Clients.FirstOrDefaultAsync(x => x.ClientId == long.Parse(decryptedId) && x.ProviderId == providerId);
        clientModel.DeleteDate = DateTime.UtcNow;

        await ExecuteWithRetry(async () =>
        {
            _db.Clients.Update(clientModel);
            await _db.SaveChangesAsync();
        });

        return response;
    }

    public async Task<List<ClientDTO>> GetClients(string jwt)
    {
        var providerId = IdentityTool
            .GetJwtClaimById(jwt, _config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var clients = await _db.Clients
            .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
            .OrderBy(x => x.LastName)
            .ToListAsync();

        return clients.Select(client =>
        {
            var dto = new ClientDTO();
            dto.CreateRecordFromModel(client);
            CryptographyTool.Encrypt(dto.ClientId, out var encryptedId);
            dto.ClientId = encryptedId;
            return dto;
        }).ToList();
    }

    private async Task<bool> ClientExists(ClientDTO dto, bool updating)
    {
        var phoneExists = _db.Clients.AnyAsync(x => x.PhoneNumber == dto.PhoneNumber && x.DeleteDate == null);
        var nameExists = _db.Clients.AnyAsync(x =>
            x.FirstName == dto.FirstName &&
            x.MiddleName == dto.MiddleName &&
            x.LastName == dto.LastName &&
            x.DeleteDate == null);
        
        await Task.WhenAll(phoneExists, nameExists);

        return phoneExists.Result || nameExists.Result;
    }

    private async Task ExecuteWithRetry(Func<Task> action)
    {
        var maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                await action();
                await transaction.CommitAsync();
                _logger.LogInfo("Database transaction committed successfully.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Attempt {attempt} failed: {ex.Message}");
                if (attempt == maxRetries)
                {
                    _logger.LogError("All attempts failed. No data was committed.");
                    throw;
                }
                await Task.Delay(retryDelay);
            }
        }
    }
}

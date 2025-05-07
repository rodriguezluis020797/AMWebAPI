using System.Diagnostics;
using System.Runtime.CompilerServices;
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

public class ClientService(IAMLogger logger, AMCoreData db, IConfiguration config) : IClientService
{
    public async Task<ClientDTO> CreateClient(ClientDTO dto, string jwt)
    {
        var response = new ClientDTO();

        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage)) return dto;

        if (await ClientExistsAsync(dto, true))
        {
            response.ErrorMessage = "A client with the given phone number or name already exists.";
            return response;
        }

        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var clientModel = new ClientModel(providerId, dto.FirstName, dto.MiddleName, dto.LastName, dto.PhoneNumber);

        var message = "Your phone number is now being used by #ProviderName# to send you automated SMS messages.\n" +
                      "Reply with 'Stop' to stop all further communication from this service.";

        var clientComm = new ClientCommunicationModel(0, message, DateTime.MinValue);

        await ExecuteWithRetryAsync(async () =>
        {
            using (var trans = await db.Database.BeginTransactionAsync())
            {
                try
                {
                    await db.Clients.AddAsync(clientModel);
                    await db.SaveChangesAsync();

                    clientComm.ClientId = clientModel.ClientId;

                    await db.ClientCommunications.AddAsync(clientComm);
                    await db.SaveChangesAsync();

                    await trans.CommitAsync();
                }
                catch (Exception ex)
                {
                    await trans.RollbackAsync();
                    throw;
                }
            }
        });

        return response;
    }

    public async Task<ClientDTO> UpdateClient(ClientDTO dto, string jwt)
    {
        var response = new ClientDTO();

        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage)) return dto;

        if (await ClientExistsAsync(dto, false))
        {
            response.ErrorMessage = "A client with the given phone number or name already exists.";
            return response;
        }

        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        CryptographyTool.Decrypt(dto.ClientId, out var decryptedId);

        var clientModel = new ClientModel(0, string.Empty, string.Empty, string.Empty, string.Empty);

        await ExecuteWithRetryAsync(async () =>
        {
            clientModel =
                await db.Clients
                    .Where(x => x.ClientId == long.Parse(decryptedId) && x.ProviderId == providerId)
                    .Include(x => x.Provider)
                    .FirstOrDefaultAsync();
        });

        var message = "Your phone number is now being used by #ProviderName# to send you automated SMS messages.\n" +
                      "Reply with 'Stop' to stop all further communication from this service.";

        var clientComm = new ClientCommunicationModel(0, message, DateTime.MinValue);
        var addCom = !clientModel.PhoneNumber.Equals(dto.PhoneNumber);

        clientModel.UpdateRecordFromDTO(dto);

        await ExecuteWithRetryAsync(async () =>
        {
            using (var trans = await db.Database.BeginTransactionAsync())
            {
                try
                {
                    if (addCom)
                    {
                        clientComm.ClientId = clientModel.ClientId;
                        await db.ClientCommunications.AddAsync(clientComm);
                        await db.SaveChangesAsync();
                    }

                    db.Clients.Update(clientModel);
                    await db.SaveChangesAsync();
                    await trans.CommitAsync();
                }
                catch (Exception ex)
                {
                    await trans.RollbackAsync();
                    throw;
                }
            }
        });

        return response;
    }

    public async Task<ClientDTO> DeleteClient(ClientDTO dto, string jwt)
    {
        var response = new ClientDTO();

        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        CryptographyTool.Decrypt(dto.ClientId, out var decryptedId);

        var clientModel =
            await db.Clients.FirstOrDefaultAsync(x =>
                x.ClientId == long.Parse(decryptedId) && x.ProviderId == providerId);
        clientModel.DeleteDate = DateTime.UtcNow;

        await ExecuteWithRetryAsync(async () =>
        {
            db.Clients.Update(clientModel);
            await db.SaveChangesAsync();
        });

        return response;
    }

    public async Task<List<ClientDTO>> GetClients(string jwt)
    {
        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var clients = new List<ClientModel>();

        await ExecuteWithRetryAsync(async () =>
        {
            clients = await db.Clients
                .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
                .OrderBy(x => x.LastName)
                .ToListAsync();
        });

        var clientDtoList = new List<ClientDTO>();

        foreach (var client in clients)
        {
            var dto = new ClientDTO();
            dto.CreateRecordFromModel(client);
            CryptographyTool.Encrypt(dto.ClientId, out var encryptedId);
            dto.ClientId = encryptedId;
            clientDtoList.Add(dto);
        }

        return clientDtoList;
    }

    private async Task<bool> ClientExistsAsync(ClientDTO dto, bool isNewRecord)
    {
        var exists = false;
        var decryptedId = string.Empty;
        if (!isNewRecord) CryptographyTool.Decrypt(dto.ClientId, out decryptedId);

        await ExecuteWithRetryAsync(async () =>
            {
                if (isNewRecord)
                    exists = await db.Clients.AnyAsync(x =>
                        x.DeleteDate == null &&
                        (
                            x.PhoneNumber == dto.PhoneNumber ||
                            (
                                x.FirstName == dto.FirstName &&
                                x.MiddleName == dto.MiddleName &&
                                x.LastName == dto.LastName
                            )
                        )
                    );
                else
                    exists = await db.Clients
                        .Where(x => x.DeleteDate == null &&
                                    x.ClientId != long.Parse(decryptedId) &&
                                    (x.PhoneNumber == dto.PhoneNumber ||
                                     (x.FirstName == dto.FirstName &&
                                      x.MiddleName == dto.MiddleName &&
                                      x.LastName == dto.LastName)))
                        .AnyAsync();
            }
        );
        return exists;
    }

    private async Task ExecuteWithRetryAsync(Func<Task> action, [CallerMemberName] string callerName = "")
    {
        var stopwatch = Stopwatch.StartNew();
        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(2);
        var attempt = 0;

        for (attempt = 1; attempt <= maxRetries; attempt++)
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                if (attempt == maxRetries)
                {
                    logger.LogError(ex.ToString());
                    throw;
                }

                await Task.Delay(retryDelay);
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInfo(
                    $"{callerName}: {nameof(ExecuteWithRetryAsync)} took {stopwatch.ElapsedMilliseconds} ms with {attempt} attempt(s).");
            }
    }
}
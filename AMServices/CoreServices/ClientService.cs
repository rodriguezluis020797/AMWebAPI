using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMServices.DataServices;
using AMTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMServices.CoreServices;

public interface IClientService
{
    Task<ClientDTO> CreateClient(ClientDTO client, string jwt);
    Task<ClientDTO> DeleteClient(ClientDTO dto, string jwt);
    Task<List<ClientDTO>> GetClients(string jwt);
    Task<ClientDTO> UpdateClient(ClientDTO client, string jwt);
    Task<ClientNoteDTO> CreateClientNote(ClientNoteDTO dto);
    Task<List<ClientNoteDTO>> GetClientNotes(ClientDTO dto, string jwt);
    Task<BaseDTO> UpdateClientNote(ClientNoteDTO dto);
    Task<BaseDTO> DeleteClientNote(ClientNoteDTO dto);
}

public class ClientService(AMCoreData db, IConfiguration config) : IClientService
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
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        var clientModel = new ClientModel(providerId, dto.FirstName, dto.MiddleName, dto.LastName, dto.PhoneNumber);

        var clientComm = new ClientCommunicationModel(0,
            "Your phone number is now being used by #ProviderName# to send you automated SMS messages.\n" +
            "Reply with 'Stop' to stop all further communication from this service."
            , DateTime.MinValue);

        await db.ExecuteWithRetryAsync(async () =>
        {
            await using var trans = await db.Database.BeginTransactionAsync();
            try
            {
                await db.Clients.AddAsync(clientModel);
                await db.SaveChangesAsync();

                clientComm.ClientId = clientModel.ClientId;

                await db.ClientCommunications.AddAsync(clientComm);
                await db.SaveChangesAsync();

                await trans.CommitAsync();
            }
            catch (Exception)
            {
                await trans.RollbackAsync();
                throw;
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
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        CryptographyTool.Decrypt(dto.ClientId, out var decryptedId);

        var clientModel = new ClientModel(0, string.Empty, string.Empty, string.Empty, string.Empty);

        await db.ExecuteWithRetryAsync(async () =>
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

        await db.ExecuteWithRetryAsync(async () =>
        {
            await using var trans = await db.Database.BeginTransactionAsync();
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
            catch (Exception)
            {
                await trans.RollbackAsync();
                throw;
            }
        });

        return response;
    }

    public async Task<ClientNoteDTO> CreateClientNote(ClientNoteDTO dto)
    {
        var response = new ClientNoteDTO();

        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage)) return dto;

        CryptographyTool.Decrypt(dto.ClientId, out var decryptedId);
        CryptographyTool.Encrypt(dto.Note, out var encryptedNote);

        var clientNoteModel = new ClientNoteModel(long.Parse(decryptedId), encryptedNote);

        await db.ExecuteWithRetryAsync(async () =>
        {
            await db.ClientNotes.AddAsync(clientNoteModel);
            await db.SaveChangesAsync();
        });

        return response;
    }

    public async Task<List<ClientNoteDTO>> GetClientNotes(ClientDTO dto, string jwt)
    {
        var response = new List<ClientNoteDTO>();

        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        CryptographyTool.Decrypt(dto.ClientId, out var decryptedId);

        var clientNoteModels = new List<ClientNoteModel>();
        var timeZoneCode = TimeZoneCodeEnum.Select;

        await db.ExecuteWithRetryAsync(async () =>
        {
            clientNoteModels = await db.ClientNotes
                .Where(x => x.ClientId == long.Parse(decryptedId) && x.DeleteDate == null)
                .OrderByDescending(x => x.CreateDate)
                .AsNoTracking()
                .ToListAsync();

            timeZoneCode = await db.Providers
                .Where(x => x.ProviderId == providerId)
                .Select(x => x.TimeZoneCode)
                .FirstOrDefaultAsync();
        });

        var timeZoneCodeStr = timeZoneCode.ToString().Replace("_", " ");
        foreach (var clientNote in clientNoteModels)
        {
            clientNote.CreateDate = DateTimeTool.ConvertUtcToLocal(clientNote.CreateDate, timeZoneCodeStr);

            if (clientNote.UpdateDate.HasValue)
                clientNote.UpdateDate = DateTimeTool.ConvertUtcToLocal(clientNote.UpdateDate.Value, timeZoneCodeStr);

            var clientNoteDto = new ClientNoteDTO();
            clientNoteDto.CreateRecordFromModel(clientNote);

            CryptographyTool.Encrypt(clientNoteDto.ClientNoteId, out var encryptedClientNoteId);
            clientNoteDto.ClientNoteId = encryptedClientNoteId;

            CryptographyTool.Encrypt(clientNoteDto.ClientId, out var encryptedClientId);
            clientNoteDto.ClientId = encryptedClientId;

            CryptographyTool.Decrypt(clientNoteDto.Note, out var decryptedNote);
            clientNoteDto.Note = decryptedNote;

            response.Add(clientNoteDto);
        }

        return response;
    }

    public async Task<BaseDTO> UpdateClientNote(ClientNoteDTO dto)
    {
        var response = new BaseDTO();
        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage)) return response;

        CryptographyTool.Decrypt(dto.ClientNoteId, out var decryptedId);
        CryptographyTool.Encrypt(dto.Note, out var encryptedNote);

        await db.ExecuteWithRetryAsync(async () =>
        {
            await db.ClientNotes
                .Where(x => x.ClientNoteId == long.Parse(decryptedId) && x.DeleteDate == null)
                .ExecuteUpdateAsync(upd =>
                    upd.SetProperty(x => x.Note, encryptedNote)
                        .SetProperty(x => x.UpdateDate, DateTime.UtcNow));

            await db.SaveChangesAsync();
        });

        return response;
    }

    public async Task<BaseDTO> DeleteClientNote(ClientNoteDTO dto)
    {
        var response = new BaseDTO();

        CryptographyTool.Decrypt(dto.ClientNoteId, out var decryptedId);

        await db.ExecuteWithRetryAsync(async () =>
        {
            await db.ClientNotes
                .Where(x => x.ClientNoteId == long.Parse(decryptedId) && x.DeleteDate == null)
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));

            await db.SaveChangesAsync();
        });

        return response;
    }

    public async Task<ClientDTO> DeleteClient(ClientDTO dto, string jwt)
    {
        var response = new ClientDTO();

        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        CryptographyTool.Decrypt(dto.ClientId, out var decryptedId);

        var appointmentExists = false;

        await db.ExecuteWithRetryAsync(async () =>
        {
            appointmentExists = await db.Appointments
                .Where(x => x.ClientId == long.Parse(decryptedId) && x.DeleteDate == null &&
                            x.Status != AppointmentStatusEnum.Scheduled)
                .AnyAsync();

            await db.Clients.Where(x =>
                    x.ClientId == long.Parse(decryptedId) && x.ProviderId == providerId)
                .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));

            await db.SaveChangesAsync();
        });

        if (appointmentExists)
            response.ErrorMessage = "This client has appointment(s) scheduled. Please cancel the appointment(s) first.";

        return response;
    }

    public async Task<List<ClientDTO>> GetClients(string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        var clients = new List<ClientModel>();

        await db.ExecuteWithRetryAsync(async () =>
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

        await db.ExecuteWithRetryAsync(async () =>
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
}
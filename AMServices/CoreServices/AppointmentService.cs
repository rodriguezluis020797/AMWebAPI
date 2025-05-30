using System.Text.RegularExpressions;
using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMServices.CoreServices;

public interface IAppointmentService
{
    Task<AppointmentDTO> CreateAppointmentAsync(AppointmentDTO dto, string jwt);
    Task<List<AppointmentDTO>> GetAllAppointmentsAsync(string jwt);
    Task<List<AppointmentDTO>> GetUpcomingAppointmentsAsync(string jwt);
    Task<AppointmentDTO> UpdateAppointmentAsync(AppointmentDTO model, string jwt);
    Task<AppointmentDTO> DeleteAppointmentAsync(AppointmentDTO model, string jwt);
}

public class AppointmentService(IAMLogger logger, AMCoreData db, IConfiguration config) : IAppointmentService
{
    public async Task<List<AppointmentDTO>> GetAllAppointmentsAsync(string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());
        var response = new List<AppointmentDTO>();
        var appointmentModels = new List<AppointmentModel>();
        var timeZoneCode = TimeZoneCodeEnum.Select;

        await db.ExecuteWithRetryAsync(async () =>
        {
            timeZoneCode = await db.Providers
                .Where(x => x.ProviderId == providerId)
                .Select(x => x.TimeZoneCode)
                .FirstOrDefaultAsync();

            appointmentModels = await db.Appointments
                .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
                .Include(x => x.Service)
                .Include(x => x.Client)
                .ToListAsync();
        });

        foreach (var appointment in appointmentModels) response.Add(BuildEncryptedDTO(appointment, timeZoneCode));

        return response;
    }

    public async Task<List<AppointmentDTO>> GetUpcomingAppointmentsAsync(string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());
        var response = new List<AppointmentDTO>();
        var appointmentModels = new List<AppointmentModel>();
        var timeZoneCode = TimeZoneCodeEnum.Select;

        var now = DateTime.UtcNow;
        var next24Hours = now.AddHours(24);

        await db.ExecuteWithRetryAsync(async () =>
        {
            timeZoneCode = await db.Providers
                .Where(x => x.ProviderId == providerId)
                .Select(x => x.TimeZoneCode)
                .FirstOrDefaultAsync();

            appointmentModels = await db.Appointments
                .Where(x =>
                    x.ProviderId == providerId &&
                    x.DeleteDate == null &&
                    x.StartDate >= now &&
                    x.StartDate <= next24Hours)
                .Include(x => x.Service)
                .Include(x => x.Client)
                .OrderBy(x => x.StartDate)
                .ToListAsync();
        });

        foreach (var appointment in appointmentModels) response.Add(BuildEncryptedDTO(appointment, timeZoneCode));

        return response;
    }

    public async Task<AppointmentDTO> UpdateAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var message =
            $"Your appointment date and/or times with #Name# have changed. " +
            $"New start: {dto.StartDate:M/d/yyyy h:mm tt} - " +
            $"New end: {dto.EndDate:M/d/yyyy h:mm tt}.";

        var appointmentModel = new AppointmentModel();

        CryptographyTool.Decrypt(dto.AppointmentId, out var decryptedAppointmentId);

        await db.ExecuteWithRetryAsync(async () =>
        {
            appointmentModel = await db.Appointments
                .Where(x =>
                    x.ProviderId == providerId && x.AppointmentId == long.Parse(decryptedAppointmentId))
                .Include(x => x.Provider)
                .FirstOrDefaultAsync();
        });

        dto.Validate();

        if (!string.IsNullOrEmpty(dto.ErrorMessage)) return dto;

        var timeZoneCodeStr = appointmentModel.Provider.TimeZoneCode.ToString().Replace("_", " ");
        dto.StartDate = DateTimeTool.ConvertLocalToUtc(dto.StartDate, timeZoneCodeStr);
        dto.EndDate = DateTimeTool.ConvertLocalToUtc(dto.EndDate, timeZoneCodeStr);

        var timesChanged = appointmentModel.StartDate != dto.StartDate || appointmentModel.EndDate != dto.EndDate;

        if (timesChanged)
            if (await ConflictsWithExistingAppointment(dto, providerId))
            {
                dto.ErrorMessage = "This conflicts with a different appointment.";
                return dto;
            }

        appointmentModel.UpdateRecrodFromDto(dto);

        var clientComm = new ClientCommunicationModel();

        if (timesChanged)
        {
            message = message.Replace("#Name#", $"{appointmentModel.Provider.BusinessName}");
            clientComm = new ClientCommunicationModel(appointmentModel.ClientId, message, DateTime.MinValue);
        }

        await db.ExecuteWithRetryAsync(async () =>
        {
            using var transaction = await db.Database.BeginTransactionAsync();

            if (timesChanged) db.ClientCommunications.Add(clientComm);
            db.Appointments.Update(appointmentModel);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return new AppointmentDTO();
    }

    public async Task<AppointmentDTO> DeleteAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        CryptographyTool.Decrypt(dto.AppointmentId, out var decryptedAppointmentId);

        var appointmentModel = new AppointmentModel();
        var clientComm = new ClientCommunicationModel();
        var message = "Your appointment with #Name# on #Date# at #Time# has been canceled.";

        await db.ExecuteWithRetryAsync(async () =>
        {
            appointmentModel = await db.Appointments
                .Where(x => x.ProviderId == providerId && x.AppointmentId == long.Parse(decryptedAppointmentId))
                .Include(x => x.Provider)
                .FirstOrDefaultAsync();

            message = message
                .Replace("#Name#", $"{appointmentModel.Provider.BusinessName}")
                .Replace("#Date#", $"{appointmentModel.StartDate:M/d/yyyy}")
                .Replace("#Time#", $"{appointmentModel.StartDate:h:mm tt}");

            clientComm = new ClientCommunicationModel(appointmentModel.ClientId, message, DateTime.MinValue);

            using var transaction = await db.Database.BeginTransactionAsync();
            appointmentModel.DeleteDate = DateTime.UtcNow;
            db.Appointments.Update(appointmentModel);
            db.ClientCommunications.Add(clientComm);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return new AppointmentDTO();
    }

    public async Task<AppointmentDTO> CreateAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var providerTimeZone = TimeZoneCodeEnum.Select;

        await db.ExecuteWithRetryAsync(async () =>
        {
            providerTimeZone = await db.Providers
                .Where(x => x.ProviderId == providerId)
                .Select(x => x.TimeZoneCode)
                .FirstOrDefaultAsync();
        });

        var timeZoneCodeStr = providerTimeZone.ToString().Replace("_", " ");

        var message =
            $"You have a new appointment with #Name# from " +
            $"{dto.StartDate:M/d/yyyy h:mm tt} to {dto.EndDate:M/d/yyyy h:mm tt} {Regex.Replace(providerTimeZone.ToString(), "[^A-Z]", "")}.";

        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage)) return new AppointmentDTO { ErrorMessage = dto.ErrorMessage };

        dto.StartDate = DateTimeTool.ConvertLocalToUtc(dto.StartDate, timeZoneCodeStr);
        dto.EndDate = DateTimeTool.ConvertLocalToUtc(dto.EndDate, timeZoneCodeStr);

        if (await ConflictsWithExistingAppointment(dto, providerId))
            return new AppointmentDTO { ErrorMessage = "This conflicts with a different appointment." };

        CryptographyTool.Decrypt(dto.ServiceId, out var decryptedServiceId);
        CryptographyTool.Decrypt(dto.ClientId, out var decryptedClientId);


        var appointmentModel = new AppointmentModel(
            long.Parse(decryptedServiceId),
            long.Parse(decryptedClientId),
            providerId,
            dto.Status,
            dto.StartDate,
            dto.EndDate,
            dto.Notes, 0);

        var businessName = string.Empty;

        var clientComm = new ClientCommunicationModel(appointmentModel.ClientId, message, DateTime.MinValue);

        await db.ExecuteWithRetryAsync(async () =>
        {
            if (dto.OverridePrice)
                appointmentModel.Price = dto.Price;
            else
                appointmentModel.Price = await db.Services.Where(x => x.ServiceId == appointmentModel.ServiceId)
                    .Select(x => x.Price)
                    .FirstOrDefaultAsync();

            using var transaction = await db.Database.BeginTransactionAsync();
            await db.Appointments.AddAsync(appointmentModel);

            businessName = await db.Providers
                .Where(x => x.ProviderId == providerId)
                .Select(x => x.BusinessName)
                .FirstOrDefaultAsync();

            clientComm.Message = clientComm.Message.Replace("#Name#", businessName);
            await db.ClientCommunications.AddAsync(clientComm);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return new AppointmentDTO();
    }

    private async Task<bool> ConflictsWithExistingAppointment(AppointmentDTO dto, long providerId)
    {
        var conflicts = false;
        await db.ExecuteWithRetryAsync(async () =>
        {
            conflicts = await db.Appointments.Where(a =>
                    a.StartDate < dto.EndDate && a.EndDate > dto.StartDate && a.ProviderId == providerId &&
                    a.DeleteDate == null)
                .AnyAsync();
        });

        return conflicts;
    }

    private AppointmentDTO BuildEncryptedDTO(AppointmentModel model, TimeZoneCodeEnum timeZoneCode)
    {
        var dto = new AppointmentDTO();
        dto.CreateNewRecordFromModel(model);

        CryptographyTool.Encrypt(dto.AppointmentId, out var encryptedAppointmentId);
        CryptographyTool.Encrypt(dto.ServiceId, out var encryptedServiceId);
        CryptographyTool.Encrypt(dto.ClientId, out var encryptedClientId);

        dto.AppointmentId = encryptedAppointmentId;
        dto.ServiceId = encryptedServiceId;
        dto.ClientId = encryptedClientId;

        var timeZoneCodeStr = timeZoneCode.ToString().Replace("_", " ");
        dto.StartDate = DateTimeTool.ConvertUtcToLocal(dto.StartDate, timeZoneCodeStr);
        dto.EndDate = DateTimeTool.ConvertUtcToLocal(dto.EndDate, timeZoneCodeStr);

        return dto;
    }
}
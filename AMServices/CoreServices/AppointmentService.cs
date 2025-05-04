using System.Diagnostics;
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
    Task<AppointmentDTO> UpdateAppointmentAsync(AppointmentDTO model, string jwt);
    Task<AppointmentDTO> DeleteAppointmentAsync(AppointmentDTO model, string jwt);
}

public class AppointmentService(IAMLogger logger, AMCoreData db, IConfiguration config) : IAppointmentService
{
    public async Task<List<AppointmentDTO>> GetAllAppointmentsAsync(string jwt)
    {
        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var appointmentModels = await db.Appointments
            .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
            .ToListAsync();

        return appointmentModels.Select(BuildEncryptedDTO).ToList();
    }

    public async Task<AppointmentDTO> UpdateAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        CryptographyTool.Decrypt(dto.AppointmentId, out var decryptedAppointmentId);

        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage))
            return new AppointmentDTO { ErrorMessage = dto.ErrorMessage };

        var appointmentModel = await db.Appointments
            .FirstOrDefaultAsync(x =>
                x.ProviderId == providerId && x.AppointmentId == long.Parse(decryptedAppointmentId));

        var timesChanged = appointmentModel.StartDate != dto.StartDate || appointmentModel.EndDate != dto.EndDate;

        if (timesChanged && await ConflictsWithExistingAppointment(dto, providerId))
            return new AppointmentDTO { ErrorMessage = "This conflicts with a different appointment." };

        appointmentModel.UpdateRecrodFromDTO(dto);

        await ExecuteWithRetryAsync(async () =>
        {
            using var transaction = await db.Database.BeginTransactionAsync();

            db.Appointments.Update(appointmentModel);

            // TODO: Add Client Comm if times changed

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return new AppointmentDTO();
    }

    public async Task<AppointmentDTO> DeleteAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        CryptographyTool.Decrypt(dto.AppointmentId, out var decryptedAppointmentId);

        var appointmentModel = await db.Appointments
            .FirstOrDefaultAsync(x =>
                x.ProviderId == providerId && x.AppointmentId == long.Parse(decryptedAppointmentId));

        await ExecuteWithRetryAsync(async () =>
        {
            using var transaction = await db.Database.BeginTransactionAsync();
            appointmentModel.DeleteDate = DateTime.UtcNow;
            db.Appointments.Update(appointmentModel);

            // TODO: Add Client Comm

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return new AppointmentDTO();
    }

    public async Task<AppointmentDTO> CreateAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage))
            return new AppointmentDTO { ErrorMessage = dto.ErrorMessage };

        if (await ConflictsWithExistingAppointment(dto, providerId))
            return new AppointmentDTO { ErrorMessage = "This conflicts with a different appointment." };

        CryptographyTool.Decrypt(dto.ServiceId, out var decryptedServiceId);
        CryptographyTool.Decrypt(dto.ClientId, out var decryptedClientId);

        var appointmentModel = new AppointmentModel(
            long.Parse(decryptedServiceId),
            long.Parse(decryptedClientId),
            providerId,
            dto.StartDate,
            dto.EndDate,
            dto.Notes);

        await ExecuteWithRetryAsync(async () =>
        {
            using var transaction = await db.Database.BeginTransactionAsync();
            await db.Appointments.AddAsync(appointmentModel);

            // TODO: Add Client Comm

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return new AppointmentDTO();
    }

    private async Task<bool> ConflictsWithExistingAppointment(AppointmentDTO dto, long providerId)
    {
        return await db.Appointments.AnyAsync(a =>
            a.StartDate < dto.EndDate && a.EndDate > dto.StartDate && a.ProviderId == providerId &&
            a.DeleteDate == null);
    }

    private AppointmentDTO BuildEncryptedDTO(AppointmentModel model)
    {
        var dto = new AppointmentDTO();
        dto.CreateNewRecordFromModel(model);

        CryptographyTool.Encrypt(dto.AppointmentId, out var encryptedAppointmentId);
        CryptographyTool.Encrypt(dto.ServiceId, out var encryptedServiceId);
        CryptographyTool.Encrypt(dto.ClientId, out var encryptedClientId);

        dto.AppointmentId = encryptedAppointmentId;
        dto.ServiceId = encryptedServiceId;
        dto.ClientId = encryptedClientId;

        return dto;
    }

    private async Task ExecuteWithRetryAsync(Func<Task> action)
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
                logger.LogInfo($"{nameof(action.Method)}'s {nameof(ExecuteWithRetryAsync)} took {stopwatch.ElapsedMilliseconds} ms with {attempt} attempt(s).");
            }
    }
}
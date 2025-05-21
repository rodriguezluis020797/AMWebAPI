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
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());
        var response = new List<AppointmentDTO>();
        var appointmentModels = new List<AppointmentModel>();
        var timeZoneCode = TimeZoneCodeEnum.Select;

        await ExecuteWithRetryAsync(async () =>
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
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());
        var response = new List<AppointmentDTO>();
        var appointmentModels = new List<AppointmentModel>();
        var timeZoneCode = TimeZoneCodeEnum.Select;

        await ExecuteWithRetryAsync(async () =>
        {
            timeZoneCode = await db.Providers
                .Where(x => x.ProviderId == providerId)
                .Select(x => x.TimeZoneCode)
                .FirstOrDefaultAsync();

            var now = DateTime.UtcNow;
            var next24Hours = now.AddHours(24);

            appointmentModels = await db.Appointments
                .Where(x =>
                    x.ProviderId == providerId &&
                    x.DeleteDate == null &&
                    x.StartDate >= now &&
                    x.StartDate <= next24Hours)
                .Include(x => x.Service)
                .Include(x => x.Client)
                .ToListAsync();
        });

        foreach (var appointment in appointmentModels) response.Add(BuildEncryptedDTO(appointment, timeZoneCode));

        return response;
    }

    public async Task<AppointmentDTO> UpdateAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetJwtClaimById(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var appointmentModel = new AppointmentModel(0, 0, 0, DateTime.MinValue, DateTime.MinValue, string.Empty);

        CryptographyTool.Decrypt(dto.AppointmentId, out var decryptedAppointmentId);

        await ExecuteWithRetryAsync(async () =>
        {
            appointmentModel = await db.Appointments
                .Where(x =>
                    x.ProviderId == providerId && x.AppointmentId == long.Parse(decryptedAppointmentId))
                .Include(x => x.Provider)
                .FirstOrDefaultAsync();
        });

        dto = TurnAppointmentTimeToUTC(dto, appointmentModel.Provider.TimeZoneCode);

        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage)) return new AppointmentDTO { ErrorMessage = dto.ErrorMessage };

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

        var providerTimeZone = TimeZoneCodeEnum.Select;

        await ExecuteWithRetryAsync(async () =>
        {
            providerTimeZone = await db.Providers
                .Where(x => x.ProviderId == providerId)
                .Select(x => x.TimeZoneCode)
                .FirstOrDefaultAsync();
        });

        dto = TurnAppointmentTimeToUTC(dto, providerTimeZone);

        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage)) return new AppointmentDTO { ErrorMessage = dto.ErrorMessage };

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

        dto = TurnAppointmentTimeToLocal(dto, timeZoneCode);

        return dto;
    }

    private AppointmentDTO TurnAppointmentTimeToUTC(AppointmentDTO dto, TimeZoneCodeEnum timeZoneCode)
    {
        // Convert enum (e.g., America_New_York) to time zone ID string (e.g., "America/New York")
        var timeZoneCodeStr = timeZoneCode.ToString().Replace("_", " ");

        // Try to find the time zone info (consider catching errors for robustness)
        TimeZoneInfo timeZoneInfo;
        try
        {
            timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneCodeStr);
        }
        catch (TimeZoneNotFoundException ex)
        {
            logger.LogError($"{ex}");
            throw;
        }

        logger.LogInfo($"Time zone ID: {timeZoneCodeStr}");
        logger.LogInfo($"Time zone display name: {timeZoneInfo.DisplayName}");
        logger.LogInfo($"Original StartDate (local naive): {dto.StartDate}");
        logger.LogInfo($"Original EndDate (local naive): {dto.EndDate}");

        // Ensure the input DateTimes are treated as 'unspecified' local times
        var localStart = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Unspecified);
        var localEnd = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Unspecified);

        // Convert to UTC using the time zone (this will handle DST properly)
        dto.StartDate = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZoneInfo);
        dto.EndDate = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZoneInfo);

        logger.LogInfo($"Converted StartDate (UTC): {dto.StartDate}");
        logger.LogInfo($"Converted EndDate (UTC): {dto.EndDate}");

        return dto;
    }

    private AppointmentDTO TurnAppointmentTimeToLocal(AppointmentDTO dto, TimeZoneCodeEnum timeZoneCode)
    {
        // Convert enum (e.g., America_New_York) to time zone ID string (e.g., "America/New York")
        var timeZoneCodeStr = timeZoneCode.ToString().Replace("_", " ");

        // Try to find the time zone info (consider catching errors for robustness)
        TimeZoneInfo timeZoneInfo;
        try
        {
            timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneCodeStr);
        }
        catch (TimeZoneNotFoundException ex)
        {
            logger.LogError($"Failed to find time zone: {ex}");
            throw;
        }

        logger.LogInfo($"Time zone ID: {timeZoneCodeStr}");
        logger.LogInfo($"Time zone display name: {timeZoneInfo.DisplayName}");
        logger.LogInfo($"Original StartDate (UTC): {dto.StartDate}");
        logger.LogInfo($"Original EndDate (UTC): {dto.EndDate}");

        // Ensure the input DateTimes are treated as UTC
        var utcStart = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);
        var utcEnd = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc);

        // Convert from UTC to the user's local time zone (handles DST)
        dto.StartDate = TimeZoneInfo.ConvertTimeFromUtc(utcStart, timeZoneInfo);
        dto.EndDate = TimeZoneInfo.ConvertTimeFromUtc(utcEnd, timeZoneInfo);

        logger.LogInfo($"Converted StartDate (local): {dto.StartDate}");
        logger.LogInfo($"Converted EndDate (local): {dto.EndDate}");

        return dto;
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
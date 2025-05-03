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

public class AppointmentService : IAppointmentService
{
    private readonly IConfiguration _config;
    private readonly AMCoreData _db;
    private readonly IAMLogger _logger;

    public AppointmentService(IAMLogger logger, AMCoreData db, IConfiguration config)
    {
        _logger = logger;
        _db = db;
        _config = config;
    }

    public async Task<List<AppointmentDTO>> GetAllAppointmentsAsync(string jwt)
    {
        var providerId = GetProviderIdFromJwt(jwt);

        var appointmentModels = await _db.Appointments
            .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
            .ToListAsync();

        return appointmentModels.Select(BuildEncryptedDTO).ToList();
    }

    public async Task<AppointmentDTO> UpdateAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var providerId = GetProviderIdFromJwt(jwt);
        CryptographyTool.Decrypt(dto.AppointmentId, out var decryptedAppointmentId);

        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage))
            return new AppointmentDTO { ErrorMessage = dto.ErrorMessage };

        var appointmentModel = await _db.Appointments
            .FirstOrDefaultAsync(x => x.ProviderId == providerId && x.AppointmentId == long.Parse(decryptedAppointmentId));

        var timesChanged = appointmentModel.StartDate != dto.StartDate || appointmentModel.EndDate != dto.EndDate;

        if (timesChanged && await ConflictsWithExistingAppointment(dto, providerId))
            return new AppointmentDTO { ErrorMessage = "This conflicts with a different appointment." };

        appointmentModel.UpdateRecrodFromDTO(dto);

        await ExecuteWithRetryAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();

            _db.Appointments.Update(appointmentModel);

            // TODO: Add Client Comm if times changed

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return new AppointmentDTO();
    }

    public async Task<AppointmentDTO> DeleteAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var providerId = GetProviderIdFromJwt(jwt);
        CryptographyTool.Decrypt(dto.AppointmentId, out var decryptedAppointmentId);

        var appointmentModel = await _db.Appointments
            .FirstOrDefaultAsync(x => x.ProviderId == providerId && x.AppointmentId == long.Parse(decryptedAppointmentId));

        await ExecuteWithRetryAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            appointmentModel.DeleteDate = DateTime.UtcNow;
            _db.Appointments.Update(appointmentModel);

            // TODO: Add Client Comm

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return new AppointmentDTO();
    }

    public async Task<AppointmentDTO> CreateAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var providerId = GetProviderIdFromJwt(jwt);

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
            using var transaction = await _db.Database.BeginTransactionAsync();
            await _db.Appointments.AddAsync(appointmentModel);

            // TODO: Add Client Comm

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return new AppointmentDTO();
    }

    private async Task<bool> ConflictsWithExistingAppointment(AppointmentDTO dto, long providerId)
    {
        return await _db.Appointments.AnyAsync(a =>
            a.StartDate < dto.EndDate && a.EndDate > dto.StartDate && a.ProviderId == providerId && a.DeleteDate == null);
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

    private long GetProviderIdFromJwt(string jwToken)
    {
        var claims = IdentityTool.GetClaimsFromJwt(jwToken, _config["Jwt:Key"]!);
        if (!long.TryParse(claims.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value, out var providerId))
            throw new ArgumentException("Invalid provider ID in JWT.");

        return providerId;
    }

    private async Task ExecuteWithRetryAsync(Func<Task> action)
    {
        const int maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await action();
                _logger.LogInfo("All database changes completed successfully.");
                return;
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

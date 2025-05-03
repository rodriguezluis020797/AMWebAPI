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
    Task<List<AppointmentDTO>> GetAllAppointmentsAsync(string jwt);
    Task<AppointmentDTO> UpdateAppointmentAsync(AppointmentDTO model, string jwt);
}

public class AppointmentService : IAppointmentService
{
    private readonly IConfiguration _config;
    private readonly AMCoreData _db;
    private readonly IAMLogger _logger;

    public AppointmentService(
        IAMLogger logger,
        AMCoreData db,
        IConfiguration config)
    {
        _logger = logger;
        _db = db;
        _config = config;
    }

    public async Task<List<AppointmentDTO>> GetAllAppointmentsAsync(string jwt)
    {
        var response = new List<AppointmentDTO>();
        var providerId = GetProviderIdFromJwt(jwt);

        var appointmentModels = await _db.Appointments
            .Where(x => x.ProviderId == providerId && x.DeleteDate == null)
            .ToListAsync();

        var dto = new AppointmentDTO();
        foreach (var appointment in appointmentModels)
        {
            dto = new AppointmentDTO();
            dto.CreateNewRecordFromModel(appointment);
            CryptographyTool.Encrypt(dto.AppointmentId, out var encryptedAppointmentId);
            CryptographyTool.Encrypt(dto.ServiceId, out var encryptedServiceId);
            CryptographyTool.Encrypt(dto.ClientId, out var encryptedClientId);
            dto.AppointmentId = encryptedAppointmentId;
            dto.ServiceId = encryptedServiceId;
            dto.ClientId = encryptedClientId;
            response.Add(dto);
        }

        return response;
    }

    public async Task<AppointmentDTO> UpdateAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var response = new AppointmentDTO();
        var providerId = GetProviderIdFromJwt(jwt);

        CryptographyTool.Decrypt(dto.AppointmentId, out var decryptedAppointmentId);

        dto.Validate();

        if (!string.IsNullOrEmpty(dto.ErrorMessage))
        {
            response.ErrorMessage = dto.ErrorMessage;
            return response;
        }

        var appointmentModel = await _db.Appointments
            .Where(x => x.ProviderId == providerId && x.AppointmentId == long.Parse(decryptedAppointmentId))
            //.Include(x => x.Provider)
            .FirstOrDefaultAsync();

        var timesChanged = !appointmentModel.StartDate.Equals(dto.StartDate) ||
                           !appointmentModel.EndDate.Equals(dto.EndDate);

        if (timesChanged)
            if (await _db.Appointments
                    .AnyAsync(a =>
                        a.StartDate < dto.EndDate && a.EndDate > dto.StartDate && a.ProviderId == providerId &&
                        a.DeleteDate == null))
            {
                response.ErrorMessage = "This conflicts with a different appointment.";
                return response;
            }

        /*
         * TODO: Create Client Comm.
         */
        appointmentModel.UpdateRecrodFromDTO(dto);

        var maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(2);
        var attempt = 0;
        while (true)
            try
            {
                using (var transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _db.Appointments.Update(appointmentModel);

                        if (timesChanged)
                        {
                            /*
                             * TODO: Add Client Comm.
                             */
                        }

                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
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

        return response;
    }

    public async Task<AppointmentDTO> CreateAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var response = new AppointmentDTO();
        var providerId = GetProviderIdFromJwt(jwt);

        dto.Validate();

        if (!string.IsNullOrEmpty(dto.ErrorMessage))
        {
            response.ErrorMessage = dto.ErrorMessage;
            return response;
        }

        if (await _db.Appointments
                .AnyAsync(a =>
                    a.StartDate < dto.EndDate && a.EndDate > dto.StartDate && a.ProviderId == providerId &&
                    a.DeleteDate == null))
        {
            response.ErrorMessage = "This conflicts with a different appointment.";
            return response;
        }

        CryptographyTool.Decrypt(dto.ServiceId, out var decryptedServiceId);
        CryptographyTool.Decrypt(dto.ClientId, out var decryptedClientId);

        var appointmentModel = new AppointmentModel(long.Parse(decryptedServiceId), long.Parse(decryptedClientId),
            providerId, dto.StartDate, dto.EndDate, dto.Notes);

        var maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(2);
        var attempt = 0;
        while (true)
            try
            {
                using (var transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        await _db.Appointments.AddAsync(appointmentModel);

                        /*
                         * TODO: Add Client Comm and implement Task.WhenAll() for speed.
                         */

                        await _db.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
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

        return response;
    }

    private long GetProviderIdFromJwt(string jwToken)
    {
        var claims = IdentityTool.GetClaimsFromJwt(jwToken, _config["Jwt:Key"]!);
        if (!long.TryParse(claims.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value, out var providerId))
            throw new ArgumentException("Invalid provider ID in JWT.");

        return providerId;
    }
}
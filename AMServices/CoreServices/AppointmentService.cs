using System.Text.RegularExpressions;
using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMServices.DataServices;
using AMTools;
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

public class AppointmentService(AMCoreData db, IConfiguration config) : IAppointmentService
{
    public async Task<List<AppointmentDTO>> GetAllAppointmentsAsync(string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));
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
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();
        });

        foreach (var appointment in appointmentModels) response.Add(BuildEncryptedDTO(appointment, timeZoneCode));

        return response;
    }

    public async Task<List<AppointmentDTO>> GetUpcomingAppointmentsAsync(string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));
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
                    x.StartDate <= next24Hours &&
                    x.Status == AppointmentStatusEnum.Scheduled)
                .Include(x => x.Service)
                .Include(x => x.Client)
                .OrderBy(x => x.StartDate)
                .ToListAsync();
        });

        return appointmentModels.Select(appointment => BuildEncryptedDTO(appointment, timeZoneCode)).ToList();
    }

    public async Task<AppointmentDTO> UpdateAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));


        var appointmentModel = new AppointmentModel();
        var clientComm = new ClientCommunicationModel();

        CryptographyTool.Decrypt(dto.AppointmentId, out var decryptedAppointmentId);
        dto.AppointmentId = decryptedAppointmentId;


        var providerTimeZone = TimeZoneCodeEnum.Select;
        await db.ExecuteWithRetryAsync(async () =>
        {
            providerTimeZone = await db.Providers
                .Where(x => x.ProviderId == providerId)
                .Select(x => x.TimeZoneCode)
                .FirstOrDefaultAsync();
        });

        var timeZoneCodeStr = providerTimeZone.ToString().Replace("_", " ");

        switch (dto.Status)
        {
            case AppointmentStatusEnum.Cancelled:
                var message = "Your appointment with #Name# on #Date# at #Time# has been canceled.";

                await db.ExecuteWithRetryAsync(async () =>
                {
                    appointmentModel = await db.Appointments
                        .Where(x => x.ProviderId == providerId && x.AppointmentId == long.Parse(decryptedAppointmentId))
                        .Include(x => x.Provider)
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                    if (appointmentModel == null) throw new Exception("Appointment could not be found.");

                    var startTimeLocal = DateTimeTool.ConvertUtcToLocal(appointmentModel.StartDate, timeZoneCodeStr);

                    message = message
                        .Replace("#Name#", $"{appointmentModel.Provider.BusinessName}")
                        .Replace("#Date#", $"{startTimeLocal:M/d/yyyy}")
                        .Replace("#Time#", $"{startTimeLocal:h:mm tt}");

                    clientComm = new ClientCommunicationModel(appointmentModel.ClientId, message, DateTime.MinValue);

                    await using var transaction = await db.Database.BeginTransactionAsync();

                    await db.Appointments
                        .Where(x => x.AppointmentId == long.Parse(decryptedAppointmentId))
                        .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.Status, AppointmentStatusEnum.Cancelled));

                    await db.ClientCommunications.AddAsync(clientComm);

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                });
                return dto;
            case AppointmentStatusEnum.Completed:

                await db.ExecuteWithRetryAsync(async () =>
                {
                    appointmentModel = await db.Appointments
                        .Where(x => x.ProviderId == providerId && x.AppointmentId == long.Parse(decryptedAppointmentId))
                        .Include(x => x.Provider)
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                    if (appointmentModel == null) throw new Exception("Appointment could not be found.");

                    var providerReview =
                        new ProviderReviewModel(appointmentModel.ProviderId, appointmentModel.ClientId);

                    message = $"You recently had an appointment with #Name#. " +
                              $"Please follow this link to give an honest review. " +
                              $"{config["URIs:AngularURI"]!}/provider-review?guid={providerReview.GuidQuery}";

                    message = message
                        .Replace("#Name#", $"{appointmentModel.Provider.BusinessName}");

                    clientComm = new ClientCommunicationModel(appointmentModel.ClientId, message,
                        DateTime.UtcNow.AddHours(1));

                    await using var transaction = await db.Database.BeginTransactionAsync();

                    await db.ProviderReviews.AddAsync(providerReview);

                    await db.Appointments
                        .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.Status, AppointmentStatusEnum.Completed));
                    await db.ClientCommunications.AddAsync(clientComm);

                    await db.SaveChangesAsync();

                    await transaction.CommitAsync();
                });

                return dto;
            case AppointmentStatusEnum.Scheduled:
            {
                dto.StartDate = DateTimeTool.ConvertLocalToUtc(dto.StartDate, timeZoneCodeStr);
                if (dto.EndDate != null)
                {
                    var dateTimeCopy = dto.EndDate.Value;
                    dto.EndDate = DateTimeTool.ConvertLocalToUtc(dateTimeCopy, timeZoneCodeStr);
                }

                dto.Validate();
                if (!string.IsNullOrEmpty(dto.ErrorMessage)) return dto;

                await db.ExecuteWithRetryAsync(async () =>
                {
                    appointmentModel = await db.Appointments
                        .Where(x =>
                            x.ProviderId == providerId && x.AppointmentId == long.Parse(decryptedAppointmentId))
                        .FirstOrDefaultAsync();
                });

                dto.Validate();
                if (!string.IsNullOrEmpty(dto.ErrorMessage)) return dto;

                var timesChanged = appointmentModel.StartDate != dto.StartDate ||
                                   appointmentModel.EndDate != dto.EndDate;

                if (timesChanged)
                {
                    if (await ConflictsWithExistingAppointment(dto, providerId))
                    {
                        dto.ErrorMessage = "This conflicts with a different appointment.";
                        return dto;
                    }

                    message = $"Your appointment date and/or times with #Name# have changed. " +
                              $"New start: {DateTimeTool.ConvertUtcToLocal(dto.StartDate, timeZoneCodeStr):M/d/yyyy h:mm tt}";
                    clientComm = new ClientCommunicationModel(appointmentModel.ClientId, message, DateTime.MinValue);
                }

                appointmentModel.UpdateRecrodFromDto(dto);

                await db.ExecuteWithRetryAsync(async () =>
                {
                    await using var transaction = await db.Database.BeginTransactionAsync();

                    if (timesChanged) db.ClientCommunications.Add(clientComm);

                    db.Appointments.Update(appointmentModel);

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                });

                return new AppointmentDTO();
            }
            case AppointmentStatusEnum.Select:
            default:
                dto.ErrorMessage = "Please select appointment status.";
                return dto;
        }
    }

    public async Task<AppointmentDTO> DeleteAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

        CryptographyTool.Decrypt(dto.AppointmentId, out var decryptedAppointmentId);

        await db.ExecuteWithRetryAsync(async () =>
        {
            var appointmentModel = await db.Appointments
                .Where(x => x.ProviderId == providerId && x.AppointmentId == long.Parse(decryptedAppointmentId))
                .Include(x => x.Provider)
                .FirstOrDefaultAsync();

            if (appointmentModel == null) throw new Exception("Appointment could not be found.");

            await using var transaction = await db.Database.BeginTransactionAsync();

            appointmentModel.DeleteDate = DateTime.UtcNow;
            db.Appointments.Update(appointmentModel);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return new AppointmentDTO();
    }

    public async Task<AppointmentDTO> CreateAppointmentAsync(AppointmentDTO dto, string jwt)
    {
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

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

        dto.StartDate = DateTimeTool.ConvertLocalToUtc(dto.StartDate, timeZoneCodeStr);
        if (dto.EndDate != null)
        {
            var dateTime = dto.EndDate.Value;
            dto.EndDate = DateTimeTool.ConvertLocalToUtc(dateTime, timeZoneCodeStr);
        }

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
            dto.Status,
            dto.StartDate,
            dto.EndDate,
            dto.Notes, 0);


        var clientComm = new ClientCommunicationModel(appointmentModel.ClientId, message, DateTime.MinValue);

        await db.ExecuteWithRetryAsync(async () =>
        {
            if (dto.OverridePrice)
                appointmentModel.Price = dto.Price;
            else
                appointmentModel.Price = await db.Services.Where(x => x.ServiceId == appointmentModel.ServiceId)
                    .Select(x => x.Price)
                    .FirstOrDefaultAsync();

            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Appointments.AddAsync(appointmentModel);

            var businessName = await db.Providers
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

        try
        {
            await db.ExecuteWithRetryAsync(async () =>
            {
                if (string.IsNullOrEmpty(dto.AppointmentId))
                {
                    conflicts = await db.Appointments.Where(a =>
                            a.StartDate < dto.EndDate &&
                            a.EndDate > dto.StartDate &&
                            a.ProviderId == providerId &&
                            a.Status == AppointmentStatusEnum.Scheduled &&
                            a.DeleteDate == null)
                        .AnyAsync();
                }
                else
                {
                    var currentId = long.Parse(dto.AppointmentId);
                    if (dto.EndDate != null)
                    {
                        conflicts = await db.Appointments.Where(a =>
                                a.StartDate < dto.EndDate &&
                                a.EndDate > dto.StartDate &&
                                a.ProviderId == providerId &&
                                a.DeleteDate == null &&
                                a.Status == AppointmentStatusEnum.Scheduled &&
                                a.DeleteDate == null &&
                                a.AppointmentId != currentId)
                            .AnyAsync();
                    }
                    else
                    {
                        var start = dto.StartDate;
                        var end = dto.StartDate.AddMinutes(1);

                        conflicts = await db.Appointments.Where(a =>
                                a.StartDate >= start &&
                                a.StartDate < end &&
                                a.ProviderId == providerId &&
                                a.DeleteDate == null &&
                                a.Status == AppointmentStatusEnum.Scheduled &&
                                a.AppointmentId != currentId)
                            .AnyAsync();
                    }
                }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            throw;
        }

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
        if (dto.EndDate != null)
        {
            var dateTimeCopy = dto.EndDate.Value;
            dto.EndDate = DateTimeTool.ConvertUtcToLocal(dateTimeCopy, timeZoneCodeStr);
        }

        return dto;
    }
}
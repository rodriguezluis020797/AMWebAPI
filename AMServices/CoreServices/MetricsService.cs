using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMServices.CoreServices;

public interface IMetricsService
{
    public Task<MetricsDTO> GetMetricsByRange(string jwt, MetricsDTO dto);
}

public class MetricsService(IAMLogger logger, AMCoreData db, IConfiguration config) : IMetricsService
{
    public async Task<MetricsDTO> GetMetricsByRange(string jwt, MetricsDTO dto)
    {
        try
        {
            var result = new MetricsDTO
            {
                StartDate = dto.StartDate,
                EndDate = dto.EndDate
            };

            var providerId = IdentityTool
                .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

            var appointments = new List<AppointmentModel>();
            var services = new List<ServiceModel>();
            var serviceIds = new List<long>();
            var providerTimeZone = TimeZoneCodeEnum.Select;
            var timeZoneString = string.Empty;

            await db.ExecuteWithRetryAsync(async () =>
            {
                providerTimeZone = await db.Providers
                    .Where(x => x.ProviderId == providerId)
                    .Select(x => x.TimeZoneCode)
                    .FirstOrDefaultAsync();
            });


            timeZoneString = providerTimeZone.ToString().Replace("_", " ");

            dto.StartDate = DateTimeTool.ConvertLocalToUtc(dto.StartDate, timeZoneString);
            dto.EndDate = DateTimeTool.ConvertLocalToUtc(dto.EndDate.AddDays(1), timeZoneString);

            dto.Validate();

            if (!string.IsNullOrEmpty(dto.ErrorMessage)) return dto;

            await db.ExecuteWithRetryAsync(async () =>
            {
                appointments = await db.Appointments
                    .Where(x => x.ProviderId == providerId &&
                                (x.Status == AppointmentStatusEnum.Completed ||
                                 x.Status == AppointmentStatusEnum.Scheduled) &&
                                dto.StartDate <= x.StartDate &&
                                x.StartDate < dto.EndDate &&
                                x.DeleteDate == null)
                    .Include(x => x.Client)
                    .AsNoTracking()
                    .ToListAsync();

                serviceIds = appointments
                    .Select(x => x.ServiceId)
                    .Distinct()
                    .ToList();

                services = await db.Services
                    .Where(s => serviceIds.Contains(s.ServiceId))
                    .AsNoTracking()
                    .ToListAsync();
            });

            appointments
                .ForEach(a => a.Service = services.FirstOrDefault(s => s.ServiceId == a.ServiceId));

            foreach (var appointment in appointments) result.CreateNewRecordFromModel(appointment);

            timeZoneString = providerTimeZone.ToString().Replace("_", " ");

            foreach (var appDto in result.Appointments)
            {
                appDto.StartDate = DateTimeTool.ConvertUtcToLocal(appDto.StartDate, timeZoneString);
                appDto.EndDate = DateTimeTool.ConvertUtcToLocal(appDto.EndDate, timeZoneString);
                CryptographyTool.Encrypt(appDto.ServiceId, out var encryptedText);
                appDto.ServiceId = encryptedText;
            }

            foreach (var ServiceName in result.ServiceNames)
            {
                CryptographyTool.Encrypt(ServiceName.Value, out var encryptedText);
                result.ServiceNames[ServiceName.Key] = encryptedText;
            }

            result.CalculateMetrics();

            return result;
        }
        catch (Exception ex)
        {
            Console.Write(ex.ToString());
            throw;
        }
    }
}
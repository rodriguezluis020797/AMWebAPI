using AMData.Models;
using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMServices.DataServices;
using AMTools;
using MCCDotnetTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMServices.CoreServices;

public interface IMetricsService
{
    public Task<MetricsDTO> GetMetricsByRange(string jwt, MetricsDTO dto);
}

public class MetricsService(AMCoreData db, IConfiguration config) : IMetricsService
{
    public async Task<MetricsDTO> GetMetricsByRange(string jwt, MetricsDTO dto)
    {
        var result = new MetricsDTO
            {
                StartDate = dto.StartDate,
                EndDate = dto.EndDate
            };

            var providerId = IdentityTool
                .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, nameof(SessionClaimEnum.ProviderId));

            var appointments = new List<AppointmentModel>();
            var services = new List<ServiceModel>();
            var providerTimeZone = TimeZoneCodeEnum.Select;

            await db.ExecuteWithRetryAsync(async () =>
            {
                providerTimeZone = await db.Providers
                    .Where(x => x.ProviderId == providerId)
                    .Select(x => x.TimeZoneCode)
                    .FirstOrDefaultAsync();
            });


            var timeZoneString = providerTimeZone.ToString().Replace("_", " ");

            dto.StartDate = MCCDateTimeTool.ConvertLocalToUtc(dto.StartDate, timeZoneString);
            dto.EndDate = MCCDateTimeTool.ConvertLocalToUtc(dto.EndDate.AddDays(1), timeZoneString);

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

                var serviceIds = appointments
                    .Select(x => x.ServiceId)
                    .Distinct()
                    .ToList();

                services = await db.Services
                    .Where(s => serviceIds.Contains(s.ServiceId))
                    .AsNoTracking()
                    .ToListAsync();
            });

            appointments
                .ForEach(a => a.Service = services.FirstOrDefault(s => s.ServiceId == a.ServiceId)!);

            foreach (var appointment in appointments) result.CreateNewRecordFromModel(appointment);

            timeZoneString = providerTimeZone.ToString().Replace("_", " ");

            foreach (var appDto in result.Appointments)
            {
                appDto.StartDate = MCCDateTimeTool.ConvertUtcToLocal(appDto.StartDate, timeZoneString);

                if (appDto.EndDate != null)
                {
                    var dateTimeCopy = appDto.EndDate.Value;
                    dto.EndDate = MCCDateTimeTool.ConvertUtcToLocal(dateTimeCopy, timeZoneString);
                }

                MCCCryptographyTool.Encrypt(appDto.ServiceId, out var encryptedText, config["Cryptography:Key"]!, config["Cryptography:IV"]!);
                appDto.ServiceId = encryptedText;
            }

            foreach (var ServiceName in result.ServiceNames)
            {
                MCCCryptographyTool.Encrypt(ServiceName.Value, out var encryptedText, config["Cryptography:Key"]!, config["Cryptography:IV"]!);
                result.ServiceNames[ServiceName.Key] = encryptedText;
            }

            result.CalculateMetrics();

            return result;
    }
}
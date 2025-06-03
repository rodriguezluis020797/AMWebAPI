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
        var providerId = IdentityTool
            .GetProviderIdFromJwt(jwt, config["Jwt:Key"]!, SessionClaimEnum.ProviderId.ToString());

        var appointments = new List<AppointmentModel>();
        var services = new List<ServiceModel>();
        var serviceIds = new List<long>();
        var providerTimeZone = TimeZoneCodeEnum.Select;
        var timeZoneString = string.Empty;

        dto.Validate();
        if (!string.IsNullOrEmpty(dto.ErrorMessage)) return dto;

        await db.ExecuteWithRetryAsync(async () =>
        {
            appointments = await db.Appointments
                .Where(x => x.ProviderId == providerId &&
                            (x.Status == AppointmentStatusEnum.Completed ||
                             x.Status == AppointmentStatusEnum.Scheduled))
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

            providerTimeZone = await db.Providers
                .Where(x => x.ProviderId == providerId)
                .Select(x => x.TimeZoneCode)
                .FirstOrDefaultAsync();
        });

        appointments
            .ForEach(a => a.Service = services.FirstOrDefault(s => s.ServiceId == a.ServiceId));

        foreach (var appointment in appointments) dto.CreateNewRecordFromModel(appointment);

        timeZoneString = providerTimeZone.ToString().Replace("_", " ");

        foreach (var appDto in dto.Appointments)
        {
            appDto.StartDate = DateTimeTool.ConvertUtcToLocal(appDto.StartDate, timeZoneString);
            appDto.EndDate = DateTimeTool.ConvertUtcToLocal(appDto.EndDate, timeZoneString);
        }

        return dto;
    }
}
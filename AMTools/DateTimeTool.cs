using NodaTime;
using NodaTime.TimeZones;
using TimeZoneConverter;

namespace AMTools;

public static class DateTimeTool
{
    public static DateTime ConvertUtcToLocal(DateTime utcDateTime, string timeZoneCodeStr)
    {
        try
        {
            Console.WriteLine($"Method: {nameof(ConvertUtcToLocal)}");
            Console.WriteLine($"{nameof(timeZoneCodeStr)}: {timeZoneCodeStr}");
            var ianaZoneId = GetIanaTimeZoneId(timeZoneCodeStr);
            var dateTimeZone = DateTimeZoneProviders.Tzdb[ianaZoneId];

            var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc));
            var zonedDateTime = instant.InZone(dateTimeZone);

            // Convert to DateTime with Local kind (local time)
            return zonedDateTime.ToDateTimeUnspecified().SpecifyKind(DateTimeKind.Local);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static DateTime ConvertLocalToUtc(DateTime localDateTime, string timeZoneCodeStr)
    {
        try
        {
            Console.WriteLine($"Method: {nameof(ConvertLocalToUtc)}");
            Console.WriteLine($"{nameof(timeZoneCodeStr)}: {timeZoneCodeStr}");
            var ianaZoneId = GetIanaTimeZoneId(timeZoneCodeStr);
            var dateTimeZone = DateTimeZoneProviders.Tzdb[ianaZoneId];

            var local = LocalDateTime.FromDateTime(localDateTime);
            var zoned = dateTimeZone.ResolveLocal(local, Resolvers.LenientResolver);

            // Convert to UTC DateTime with DateTimeKind.Utc
            return zoned.ToDateTimeUtc();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static string GetIanaTimeZoneId(string windowsTimeZoneId)
    {
        windowsTimeZoneId = windowsTimeZoneId.Trim();

        if (string.IsNullOrWhiteSpace(windowsTimeZoneId))
            throw new ArgumentException("Time zone ID is null or empty");

        try
        {
            return TZConvert.WindowsToIana(windowsTimeZoneId); // Always convert
        }
        catch
        {
            var fallbackMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Hawaii Standard Time", "Pacific/Honolulu" },
                { "Alaska Standard Time", "America/Anchorage" },
                { "Pacific Standard Time", "America/Los_Angeles" },
                { "Mountain Standard Time", "America/Denver" },
                { "Central Standard Time", "America/Chicago" },
                { "Eastern Standard Time", "America/New_York" },
                { "UTC", "Etc/UTC" }
            };

            if (fallbackMap.TryGetValue(windowsTimeZoneId, out var ianaId))
                return ianaId;

            throw new InvalidTimeZoneException($"Unknown or unmapped Windows time zone ID: {windowsTimeZoneId}");
        }
    }
}

public static class DateTimeExtensions
{
    public static DateTime SpecifyKind(this DateTime dateTime, DateTimeKind kind)
    {
        return DateTime.SpecifyKind(dateTime, kind);
    }
}
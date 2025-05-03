using AMTools.Tools;
using AMWebAPI.Services.DataServices;

namespace AMServices.CoreServices;

public interface ISystemStatusService
{
    Task<bool> IsFullSystemActive();
}

public class SystemStatusService : ISystemStatusService
{
    private readonly AMCoreData _amCoreData;
    private readonly AMIdentityData _amIdentityData;
    private readonly IAMLogger _logger;

    public SystemStatusService(IAMLogger logger, AMCoreData amCoreData, AMIdentityData amIdentityData)
    {
        _logger = logger;
        _amCoreData = amCoreData;
        _amIdentityData = amIdentityData;
    }

    public async Task<bool> IsFullSystemActive()
    {
        try
        {
            var checks = new List<(string Name, Func<bool> Check)>
            {
                (nameof(CheckCoreDb), CheckCoreDb),
                (nameof(CheckIdentityDb), CheckIdentityDb)
            };

            var tasks = checks.Select(c => Task.Run(() => (c.Name, Success: c.Check()))).ToList();
            var results = await Task.WhenAll(tasks);

            var allSuccessful = true;

            foreach (var (name, success) in results)
                if (!success)
                {
                    _logger.LogError($"System check '{name}' failed.");
                    allSuccessful = false;
                }

            return allSuccessful;
        }
        catch (Exception ex)
        {
            _logger.LogError($"System status check failed: {ex}");
            return false;
        }
    }

    private bool CheckCoreDb()
    {
        return _amCoreData.Database.CanConnect();
    }

    private bool CheckIdentityDb()
    {
        return _amIdentityData.Database.CanConnect();
    }
}
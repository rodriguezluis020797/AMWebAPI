using AMServices.DataServices;
using AMTools;
using AMTools.Tools;

namespace AMServices.CoreServices;

public interface ISystemStatusService
{
    Task<bool> IsFullSystemActive();
}

public class SystemStatusService(IAMLogger logger, AMCoreData coreData, AMIdentityData identityData)
    : ISystemStatusService
{
    public async Task<bool> IsFullSystemActive()
    {
        var coreTask = coreData.Database.CanConnectAsync();
        var identityTask = identityData.Database.CanConnectAsync();

        await coreData.ExecuteWithRetryAsync(async () => { await Task.WhenAll(coreTask, identityTask); });

        var results = new Dictionary<string, bool>
        {
            { "CoreDB", coreTask.Result },
            { "IdentityDB", identityTask.Result }
        };

        var allSuccessful = true;

        foreach (var (name, success) in results)
        {
            if (success) continue;
            logger.LogError($"System check '{name}' failed.");
            allSuccessful = false;
        }

        return allSuccessful;
    }
}
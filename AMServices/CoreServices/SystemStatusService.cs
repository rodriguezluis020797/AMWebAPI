using System.Diagnostics;
using System.Runtime.CompilerServices;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;

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

        await ExecuteWithRetryAsync(async () => { await Task.WhenAll(coreTask, identityTask); });

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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;

namespace AMServices.CoreServices
{
    public interface ISystemStatusService
    {
        Task<bool> IsFullSystemActive();
    }

    public class SystemStatusService : ISystemStatusService
    {
        private readonly AMCoreData _coreData;
        private readonly AMIdentityData _identityData;
        private readonly IAMLogger _logger;

        public SystemStatusService(IAMLogger logger, AMCoreData coreData, AMIdentityData identityData)
        {
            _logger = logger;
            _coreData = coreData;
            _identityData = identityData;
        }

        public async Task<bool> IsFullSystemActive()
        {
            try
            {
                var coreTask = _coreData.Database.CanConnectAsync();
                var identityTask = _identityData.Database.CanConnectAsync();

                await Task.WhenAll(coreTask, identityTask);

                var results = new Dictionary<string, bool>
                {
                    { "CoreDB", coreTask.Result },
                    { "IdentityDB", identityTask.Result }
                };

                var allSuccessful = true;

                foreach (var (name, success) in results)
                {
                    if (!success)
                    {
                        _logger.LogError($"System check '{name}' failed.");
                        allSuccessful = false;
                    }
                }

                return allSuccessful;
            }
            catch (Exception ex)
            {
                _logger.LogError($"System status check failed: {ex}");
                return false;
            }
        }
    }
}
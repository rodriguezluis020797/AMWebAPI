using AMTools.Tools;
using AMWebAPI.Services.DataServices;

namespace AMWebAPI.Services.CoreServices
{
    public interface ISystemStatusService
    {
        public Task<bool> IsFullSystemActive();
    }

    public class SystemStatusService : ISystemStatusService
    {
        private readonly IAMLogger _logger;
        private readonly AMCoreData _amCoreData;
        private readonly AMIdentityData _amIdentityData;
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
                var tasks = new List<(string Name, Task<bool> Task)>
        {
            ($"{nameof(CheckCoreDbTask)}", Task.Run(() => CheckCoreDbTask())),
            ($"{nameof(CheckIdentityDbTask)}", Task.Run(() => CheckIdentityDbTask()))
        };

                await Task.WhenAll(tasks.Select(t => t.Task));

                var allSuccessful = true;

                foreach (var (name, task) in tasks)
                {
                    if (!task.Result)
                    {
                        _logger.LogError($"Task '{name}' failed.");
                        allSuccessful = false;
                    }
                }

                return allSuccessful;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return false;
            }
        }

        private bool CheckCoreDbTask()
        {
            try
            {
                return _amCoreData.Database.CanConnect();
            }
            catch (Exception e)
            {
                throw;
            }
        }
        private bool CheckIdentityDbTask()
        {
            try
            {
                return _amIdentityData.Database.CanConnect();
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
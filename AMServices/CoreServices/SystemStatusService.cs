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
                var checkCoreDbTask = Task.Run(() => CheckCoreDbTask());
                var checkIdentityDbTask = Task.Run(() => CheckIdentityDbTask());

                var tasks = new List<Task<bool>>()
                {
                    checkCoreDbTask,
                    checkIdentityDbTask
                };

                var taskResults = await Task.WhenAll(tasks);

                if (taskResults.All(t => t))
                {
                    return true;
                }
                else
                {
                    var index = 0;
                    foreach (var item in taskResults)
                    {
                        if (!taskResults[index])
                        {
                            _logger.LogError($"Task with index {index} failed.");
                        }
                        index++;
                    }
                    return false;
                }
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
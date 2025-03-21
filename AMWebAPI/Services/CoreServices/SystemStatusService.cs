using AMWebAPI.Models;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.DataServices;
using AMWebAPI.Tools;

namespace AMWebAPI.Services.CoreServices
{
    public interface ISystemStatusService
    {
        public Task<SystemStatusDTO> FullSystemCheck();
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
        public async Task<SystemStatusDTO> FullSystemCheck()
        {
            var response = new SystemStatusDTO();
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
                    response = new SystemStatusDTO()
                    {
                        RequestStatus = RequestStatusEnum.Success,
                    };
                }
                else
                {
                    response = new SystemStatusDTO()
                    {
                        RequestStatus = RequestStatusEnum.Error,
                    };
                    var index = 0;
                    foreach (var item in taskResults)
                    {
                        if (!taskResults[index])
                        {
                            _logger.LogError($"Task with index {index} failed.");
                        }
                        index++;
                    }
                }
            }
            catch (Exception e)
            {
                response = new SystemStatusDTO()
                {
                    RequestStatus = RequestStatusEnum.Error,
                };
                _logger.LogError(e.ToString());
            }
            return response;
        }

        private bool CheckCoreDbTask()
        {
            return _amCoreData.Database.CanConnect();
        }
        private bool CheckIdentityDbTask()
        {
            return _amIdentityData.Database.CanConnect();
        }
    }
}
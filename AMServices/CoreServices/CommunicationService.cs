using AMData.Models.CoreModels;
using AMWebAPI.Services.DataServices;

namespace AMWebAPI.Services.CoreServices
{
    public interface ICommunicationService
    {
        public Task AddUserCommunication(long userId, string message);
    }
    public class CommunicationService : ICommunicationService
    {
        private readonly AMCoreData _coreData;
        public CommunicationService(AMCoreData coreData)
        {
            _coreData = coreData;
            _coreData = coreData;
        }
        public async Task AddUserCommunication(long userId, string message)
        {
            var utcTime = DateTime.UtcNow;
            var userComm = new UserCommunicationModel()
            {
                UserId = userId,
                AttemptOne = null,
                AttemptThree = null,
                AttemptTwo = null,
                CommunicationId = 0,
                DeleteDate = null,
                Message = message,
                SendAfter = utcTime,
                CreateDate = utcTime
            };

            await _coreData.UserCommunications.AddAsync(userComm);
            await _coreData.SaveChangesAsync();
        }
    }
}

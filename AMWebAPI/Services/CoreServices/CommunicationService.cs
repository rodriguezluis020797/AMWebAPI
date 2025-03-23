using AMWebAPI.Models.CoreModels;
using AMWebAPI.Services.DataServices;

namespace AMWebAPI.Services.CoreServices
{
    public interface ICommunicationService
    {
        public void AddUserCommunication(long userId, string message);
    }
    public class CommunicationService : ICommunicationService
    {
        private readonly AMCoreData _coreData;
        public CommunicationService(AMCoreData coreData)
        {
            _coreData = coreData;
            _coreData = coreData;
        }
        public void AddUserCommunication(long userId, string message)
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

            _coreData.UserCommunications.Add(userComm);
            _coreData.SaveChanges();
        }
    }
}

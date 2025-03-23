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
        CommunicationService(AMCoreData coreData)
        {
            _coreData = coreData;
        }
        public void AddUserCommunication(long userId, string message)
        {
            var userComm = new UserCommunicationModel()
            {
                UserId = userId,
                AttemptOne = null,
                AttemptThree = null,
                AttemptTwo = null,
                CommunicationId = 0,
                DeleteDate = null,
                Message = message
            };

            _coreData.UserCommunications.Add(userComm);
        }
    }
}

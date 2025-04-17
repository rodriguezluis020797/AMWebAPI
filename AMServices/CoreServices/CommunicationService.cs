using AMData.Models.CoreModels;
using AMWebAPI.Services.DataServices;

namespace AMWebAPI.Services.CoreServices
{
    public interface ICommunicationService
    {
        public Task AddProviderCommunication(long providerId, string message);
    }
    public class CommunicationService : ICommunicationService
    {
        private readonly AMCoreData _coreData;
        public CommunicationService(AMCoreData coreData)
        {
            _coreData = coreData;
            _coreData = coreData;
        }
        public async Task AddProviderCommunication(long providerId, string message)
        {
            var utcTime = DateTime.UtcNow;
            var providerComm = new ProviderCommunicationModel()
            {
                ProviderId = providerId,
                AttemptOne = null,
                AttemptThree = null,
                AttemptTwo = null,
                CommunicationId = 0,
                DeleteDate = null,
                Message = message,
                SendAfter = utcTime,
                CreateDate = utcTime
            };

            await _coreData.ProviderCommunications.AddAsync(providerComm);
            await _coreData.SaveChangesAsync();
        }
    }
}

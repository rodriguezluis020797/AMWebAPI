using AMData.Models.CoreModels;
using AMWebAPI.Services.DataServices;

namespace AMWebAPI.Services.CoreServices
{
    public interface ICommunicationService
    {
        Task AddProviderCommunication(long providerId, string message);
    }

    public class CommunicationService : ICommunicationService
    {
        private readonly AMCoreData _coreData;

        public CommunicationService(AMCoreData coreData)
        {
            _coreData = coreData;
        }

        public async Task AddProviderCommunication(long providerId, string message)
        {
            var now = DateTime.UtcNow;

            var communication = new ProviderCommunicationModel
            {
                ProviderId = providerId,
                Message = message,
                SendAfter = now,
                CreateDate = now
            };

            await _coreData.ProviderCommunications.AddAsync(communication);
            await _coreData.SaveChangesAsync();
        }
    }
}
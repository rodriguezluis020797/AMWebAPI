using AMData.Models.CoreModels;
using SendGrid;

namespace AMCommunication
{
    public class AMProviderEmail
    {
        public ProviderCommunicationModel Communication { get; set; } = new ProviderCommunicationModel();
        public Response Response { get; set; } = default!;
    }
}

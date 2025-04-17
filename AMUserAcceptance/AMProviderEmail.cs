using AMData.Models.CoreModels;
using Azure;

namespace AMUserAcceptance
{
    public class AMProviderEmail
    {
        public ProviderCommunicationModel Communication { get; set; } = new ProviderCommunicationModel();
        public Response Response { get; set; } = default!;
    }
}
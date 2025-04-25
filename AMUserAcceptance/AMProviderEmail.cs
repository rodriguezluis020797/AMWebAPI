using AMData.Models.CoreModels;
using Azure;

namespace AMUserAcceptance
{
    public class AMProviderEmail
    {
        public ProviderCommunicationModel Communication { get; set; } //might need to add an empty contructor
        public Response Response { get; set; } = default!;
    }
}
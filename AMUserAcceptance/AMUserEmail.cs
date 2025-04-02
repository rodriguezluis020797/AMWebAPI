using AMData.Models.CoreModels;
using Azure;

namespace AMUserAcceptance
{
    public class AMUserEmail
    {
        public UserCommunicationModel Communication { get; set; } = new UserCommunicationModel();
        public Response Response { get; set; } = default!;
    }
}
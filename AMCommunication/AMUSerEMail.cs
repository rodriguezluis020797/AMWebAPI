using AMData.Models.CoreModels;
using SendGrid;

namespace AMCommunication
{
    public class AMUserEmail
    {
        public UserCommunicationModel Communication { get; set; } = new UserCommunicationModel();
        public Response Response { get; set; } = default!;
    }
}

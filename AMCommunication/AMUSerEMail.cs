using AMData.Models.CoreModels;
using SendGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMCommunication
{
    public class AMUserEmail
    {
        public UserCommunicationModel Communication { get; set; } = new UserCommunicationModel();
        public Response Response { get; set; } = default!;
    }
}

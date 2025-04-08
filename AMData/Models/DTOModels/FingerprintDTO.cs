using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMData.Models.DTOModels
{
    public class FingerprintDTO
    {
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
        public string Platform { get; set; }
        public string Language { get; set; }
        public void Validate()
        {
            if (string.IsNullOrEmpty(IPAddress))
            {
                IPAddress = string.Empty;
            }
            if (string.IsNullOrEmpty(UserAgent))
            {
                UserAgent = string.Empty;
            }
            if (string.IsNullOrEmpty(Platform))
            {
                Platform = string.Empty;
            }
            if (string.IsNullOrEmpty(Language))
            {
                Language = string.Empty;
            }
        }
    }
}

using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.DataServices;
using AMWebAPI.Tools;
using Microsoft.EntityFrameworkCore;

namespace AMWebAPI.Services.IdentityServices
{
    public interface IIdentityService
    {
        public UserDTO LogIn(UserDTO dto);
    }
    public class IdentityService : IIdentityService
    {
        private readonly IAMLogger _logger;
        private readonly AMCoreData _coreData;
        public IdentityService(AMCoreData coreData, IAMLogger logger)
        {
            _logger = logger;
            _coreData = coreData;
        }
        public UserDTO LogIn(UserDTO dto)
        {
            var user = _coreData.Users
                .Where(x => x.EMail.Equals(dto.EMail))
                .FirstOrDefault();

            if (user == null)
            {
                dto.Password = string.Empty;
                dto.RequestStatus = Models.RequestStatusEnum.BadRequest;
                return dto;
            }

            var decryptedPassword = string.Empty; //get after setting up identity server

            if (!dto.Password.Equals(decryptedPassword))
            {
                    dto.Password = string.Empty;
                    dto.RequestStatus = Models.RequestStatusEnum.BadRequest;
                    return dto;
            }
            return null;
        }
    }
}

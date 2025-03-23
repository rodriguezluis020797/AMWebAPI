using AMWebAPI.Models;
using AMWebAPI.Models.CoreModels;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.DataServices;
using AMWebAPI.Services.IdentityServices;
using AMWebAPI.Tools;

namespace AMWebAPI.Services.CoreServices
{
    public interface IUserService
    {
        public UserDTO AddUser(UserDTO dto);
        public UserDTO GetUserById(string userId);
        public UserDTO GetUserByEMail(string eMail);
    }
    public class UserService : IUserService
    {
        private readonly IAMLogger _logger;
        private readonly AMCoreData _amCoreData;
        public UserService(IAMLogger logger, AMCoreData amCoreData)
        {
            _logger = logger;
            _amCoreData = amCoreData;
        }

        public UserDTO AddUser(UserDTO dto)
        {
            dto.Validate();
            if (!string.IsNullOrEmpty(dto.ErrorMessage))
            {
                dto.RequestStatus = RequestStatusEnum.BadRequest;
                return dto;
            }
            if (_amCoreData.Users.Any(x => x.EMail.Equals(dto.EMail)))
            {
                dto.RequestStatus = RequestStatusEnum.BadRequest;
                dto.ErrorMessage = $"User with given e-mail already exists.{Environment.NewLine}" +
                    $"Please wait to be given access.";
                return dto;
            }
            else
            {
                var user = new UserModel();
                user.CreateNewRecordFromDTO(dto);
                _amCoreData.Users.Add(user);
                //Add communication
                _amCoreData.SaveChanges();

                _logger.LogAudit($"User Id: {user.UserId}{Environment.NewLine}E-Mail: {user.EMail}");

                dto.CreateNewRecordFromModel(user);
                dto.RequestStatus = RequestStatusEnum.Success;
                return dto;
            }
        }

        public UserDTO GetUserByEMail(string eMail)
        {
            var dto = new UserDTO();

            var user = _amCoreData.Users
                .Where(x => x.EMail.Equals(eMail))
                .FirstOrDefault();

            if (user == null)
            {
                dto.ErrorMessage = "User Not Foound";
                dto.RequestStatus = RequestStatusEnum.BadRequest;
                return dto;
            }
            else
            {
                dto.CreateNewRecordFromModel(user);
                dto.RequestStatus = RequestStatusEnum.Success;
            }
            return dto;
        }

        public UserDTO GetUserById(string userId)
        {
            var dto = new UserDTO();

            long.TryParse(CryptographyTool.Decrypt(userId), out long result);

            var user = _amCoreData.Users
                .Where(x => x.UserId == result)
                .FirstOrDefault();

            if (user == null)
            {
                throw new ArgumentException(nameof(userId));
            }
            else
            {
                dto.CreateNewRecordFromModel(user);
                dto.RequestStatus = RequestStatusEnum.Success;
                return dto;
            }
        }
    }
}
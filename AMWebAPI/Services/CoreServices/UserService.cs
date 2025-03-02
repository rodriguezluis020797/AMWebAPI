using AMWebAPI.Models.CoreModels;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.DataServices;
using AMWebAPI.Tools;

namespace AMWebAPI.Services.CoreServices
{
    public interface IUserService
    {
        public void AddUser(UserDTO dto, out long userId, out string message);
        public void GetUser(long userId, out UserDTO dto);
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
        public void AddUser(UserDTO dto, out long userId, out string message)
        {
            userId = default;
            message = string.Empty;
            if (_amCoreData.Users.Any(x => x.EMail.Equals(dto.EMail)))
            {
                message = "User with given e-mail already exists.";
            }
            else
            {
                var user = new UserModel()
                {
                    CreateDate = DateTime.UtcNow,
                    DeleteDate = null,
                    EMail = dto.EMail,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    MiddleName = dto.MiddleName,
                    UpdateDate = null,
                    UserId = default,
                };

                _amCoreData.Users.Add(user);
                _amCoreData.SaveChanges();

                userId = user.UserId;
                _logger.LogAudit($"User Id: {userId}{Environment.NewLine}E-Mail: {dto.EMail}");
            }
        }

        public void GetUser(long userId, out UserDTO dto)
        {
            var user = _amCoreData.Users
                .Where(x => x.UserId == userId)
                .FirstOrDefault();

            if (user == null)
            {
                throw new ArgumentException(nameof(userId));
            }
            else
            {
                dto = new UserDTO()
                {
                    EMail = user.EMail,
                    ErrorMessage = string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    MiddleName = user.MiddleName,
                    RequestStatus = Models.RequestStatusEnum.Unknown,
                    UserId = Uri.EscapeDataString(EncryptionTool.Encrypt(user.UserId.ToString()))
                };
            }
        }
    }
}

using AMWebAPI.Models.CoreModels;
using AMWebAPI.Models.DTOModels.User;
using AMWebAPI.Services.DataServices;
using AMWebAPI.Tools;

namespace AMWebAPI.Services.CoreServices
{
    public interface IUserService
    {
        public void AddUser(CreateUserDTO dto, out long userId, out string message);
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
        public void AddUser(CreateUserDTO dto, out long userId, out string message)
        {
            userId = default;
            message = string.Empty;
            if (_amCoreData.Users.Any(x => x.EMail.Equals(dto.EMail)))
            {
                message = "User already exists.";
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
                    UserId = dto.UserId,
                };

                _amCoreData.Users.Add(user);
                _amCoreData.SaveChanges();

                userId = user.UserId;
                _logger.LogAudit($"User Id: {userId}{Environment.NewLine}E-Mail: {dto.EMail}");
            }
        }
    }
}

using AMWebAPI.Models.DTOModels.User;
using AMWebAPI.Tools;

namespace AMWebAPI.Services.CoreServices
{
    public interface IUserService
    {
        public void AddUser(CreateUserDTO dto, out long userId);
    }
    public class UserService : IUserService
    {
        private readonly IAMLogger _logger;
        public UserService(IAMLogger logger)
        {
            _logger = logger;
        }
        public void AddUser(CreateUserDTO dto, out long userId)
        {
            userId = 0;
            userId++;
            _logger.LogAudit($"User Id: {userId}{Environment.NewLine}E-Mail: {dto.EMail}");
        }
    }
}

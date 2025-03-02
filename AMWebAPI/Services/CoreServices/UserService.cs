using AMWebAPI.Models.DTOModels.User;

namespace AMWebAPI.Services.CoreServices
{
    public interface IUserService
    {
        public void AddUser(CreateUserDTO dto, out long userId);
    }
    public class UserService : IUserService
    {
        public void AddUser(CreateUserDTO dto, out long userId)
        {
            userId = 0;
            try
            {
                userId++;
            }
            catch (Exception e)
            {
                //log error
            }
        }
    }
}

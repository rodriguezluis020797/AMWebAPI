using AMWebAPI.Models;
using AMWebAPI.Models.DTOModels.User;
using AMWebAPI.Services.CoreServices;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO dto)
        {
            try
            {
                dto.Validate();
                if (!string.IsNullOrEmpty(dto.ErrorMessage))
                {
                    dto.RequestStatus = RequestStatusEnum.BadRequest;
                    return new ObjectResult(dto);
                }

                _userService.AddUser(dto, out long userId);

                dto = new CreateUserDTO
                {
                    UserId = userId,
                    RequestStatus = RequestStatusEnum.Success,
                };
            }
            catch (Exception e)
            {
                dto.ErrorMessage = "Server Error.";
                dto.RequestStatus = RequestStatusEnum.Error;
                return new ObjectResult(dto);
            }
            return new ObjectResult(dto);
        }
    }
}

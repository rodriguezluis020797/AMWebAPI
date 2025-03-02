using AMWebAPI.Models;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.CoreServices;
using AMWebAPI.Tools;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAMLogger _logger;
        public UserController(IAMLogger logger, IUserService userService)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserDTO dto)
        {
            _logger.LogInfo("+");
            try
            {
                dto.Validate();
                if (!string.IsNullOrEmpty(dto.ErrorMessage))
                {
                    dto.RequestStatus = RequestStatusEnum.BadRequest;
                    return new ObjectResult(dto);
                }

                _userService.AddUser(dto, out long userId, out string message);

                if (!string.IsNullOrEmpty(message))
                {
                    dto.ErrorMessage = message;
                    dto.RequestStatus = RequestStatusEnum.BadRequest;
                    return new ObjectResult(dto);
                }

                dto = new UserDTO
                {
                    UserId = userId.ToString(),
                    RequestStatus = RequestStatusEnum.Success,
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                dto.ErrorMessage = "Server Error.";
                dto.RequestStatus = RequestStatusEnum.Error;
            }

            _logger.LogInfo("-");
            return new ObjectResult(dto);
        }

        [HttpGet]
        public async Task<IActionResult> GetUser([FromQuery] string userId)
        {
            _logger.LogInfo("+");
            var dto = new UserDTO();
            try
            {
                long.TryParse(userId, out long id);

                _userService.GetUser(id, out dto);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                dto.ErrorMessage = "Server Error.";
                dto.RequestStatus = RequestStatusEnum.Error;
            }
            _logger.LogInfo("-");
            return new ObjectResult(dto);
        }
    }
}

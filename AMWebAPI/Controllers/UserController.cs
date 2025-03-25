using AMData.Models;
using AMWebAPI.Models;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.CoreServices;
using AMWebAPI.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAMLogger _logger;
        public UserController(IAMLogger logger, IUserService userService)
        {
            _userService = userService;
            _logger = logger;
        }


        //Tested Endpoint
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateUser([FromBody] UserDTO dto)
        {
            _logger.LogInfo("+");
            var response = new UserDTO();
            try
            {
                response = _userService.CreateUser(dto);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                response = new UserDTO();
                response.ErrorMessage = "Server Error.";
                response.RequestStatus = RequestStatusEnum.Error;
            }
            _logger.LogInfo("-");
            return new ObjectResult(response);
        }


        [HttpGet]
        public async Task<IActionResult> GetUserById([FromQuery] string userId)
        {
            _logger.LogInfo("+");
            var response = new UserDTO();
            try
            {
                response = _userService.GetUserById(userId);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                response = new UserDTO();
                response.ErrorMessage = "Server Error.";
                response.RequestStatus = RequestStatusEnum.Error;
            }
            _logger.LogInfo("-");
            return new ObjectResult(response);
        }

        [HttpPost]
        public async Task<IActionResult> GetUserByEMail([FromBody] UserDTO dto)
        {
            _logger.LogInfo("+");
            var response = new UserDTO();
            try
            {
                response = _userService.GetUserByEMail(dto.EMail);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                response = new UserDTO();
                response.ErrorMessage = "Server Error.";
                response.RequestStatus = RequestStatusEnum.Error;
            }
            _logger.LogInfo("-");
            return new ObjectResult(response);
        }
    }
}

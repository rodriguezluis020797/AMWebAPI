using AMData.Models;
using AMTools.Tools;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.CoreServices;
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
            }
            _logger.LogInfo("-");
            return new ObjectResult(response);
        }


        [HttpGet]
        public async Task<IActionResult> GetUser()
        {
            _logger.LogInfo("+");
            var response = new UserDTO();

            try
            {
                var jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
                if (jwToken == null)
                {
                    throw new Exception(nameof(jwToken));
                }

                response = await _userService.GetUser(jwToken);

            }
            catch(Exception e)
            {
                _logger.LogError(e.ToString());
                _logger.LogInfo("-");
                return StatusCode((int)HttpStatusCodeEnum.ServerError);
            }

            _logger.LogInfo("-");
            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
    }
}

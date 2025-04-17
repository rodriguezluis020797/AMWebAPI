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
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAMLogger _logger;

        public UserController(IAMLogger logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateUser([FromBody] ProvidderDTO dto)
        {
            _logger.LogInfo("+");
            try
            {
                var result = await _userService.CreateUser(dto);
                return StatusCode((int)HttpStatusCodeEnum.Success, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return StatusCode((int)HttpStatusCodeEnum.ServerError);
            }
            finally
            {
                _logger.LogInfo("-");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUser()
        {
            _logger.LogInfo("+");

            try
            {
                var jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
                if (string.IsNullOrWhiteSpace(jwToken))
                    throw new Exception("JWT token missing from cookies.");

                var user = await _userService.GetUser(jwToken);
                return StatusCode((int)HttpStatusCodeEnum.Success, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return StatusCode((int)HttpStatusCodeEnum.ServerError);
            }
            finally
            {
                _logger.LogInfo("-");
            }
        }
    }
}
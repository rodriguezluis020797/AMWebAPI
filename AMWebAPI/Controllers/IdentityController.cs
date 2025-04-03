using AMData.Models;
using AMTools.Tools;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.IdentityServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class IdentityController : Controller
    {
        private readonly IAMLogger _logger;
        private readonly IIdentityService _identityService;
        public IdentityController(IAMLogger logger, IIdentityService identityService)
        {
            _logger = logger;
            _identityService = identityService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LogIn([FromBody] UserDTO dto)
        {
            _logger.LogInfo("+");
            var response = new UserDTO();
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = "Not Detected";
                }
                response = _identityService.LogIn(dto, ipAddress);
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
        public async Task<IActionResult> RefreshToken()
        {
            _logger.LogInfo("+");
            var response = new UserDTO();
            try
            {
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                response = new UserDTO();
                response.ErrorMessage = "Server Error.";
                response.RequestStatus = RequestStatusEnum.Error;
            }

            return new ObjectResult(nameof(NotImplementedException));
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] UserDTO dto)
        {
            _logger.LogInfo("+");
            var response = new UserDTO();

            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);
                response = _identityService.UpdatePassword(dto, token);

            }
            catch(UnauthorizedAccessException e)
            {
                _logger.LogError(e.ToString());
                response = new UserDTO();
                response.ErrorMessage = "Server Error.";
                response.RequestStatus = RequestStatusEnum.JWTError;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                response = new UserDTO();
                response.ErrorMessage = "Server Error.";
                response.RequestStatus = RequestStatusEnum.Error;
            }

            return new OkObjectResult(response);
        }
    }
}

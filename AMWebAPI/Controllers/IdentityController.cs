using AMData.Models;
using AMWebAPI.Models;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.IdentityServices;
using AMWebAPI.Tools;
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
    }
}

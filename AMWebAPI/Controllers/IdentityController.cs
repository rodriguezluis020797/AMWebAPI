using AMWebAPI.Models;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.CoreServices;
using AMWebAPI.Tools;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class IdentityController : Controller
    {
        private readonly IAMLogger _logger;
        public IdentityController(IAMLogger logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> LogIn([FromBody] UserDTO dto)
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
            _logger.LogInfo("-");
            return new ObjectResult(response);
        }
    }
}

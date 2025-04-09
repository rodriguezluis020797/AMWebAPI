using AMData.Models;
using AMTools.Tools;
using AMWebAPI.Services.CoreServices;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class SystemStatusController : Controller
    {

        private readonly IAMLogger _logger;
        private readonly ISystemStatusService _systemStatusService;
        public SystemStatusController(IAMLogger logger, ISystemStatusService systemStatusService)
        {
            _logger = logger;
            _systemStatusService = systemStatusService;
        }

        [HttpGet]
        public async Task<IActionResult> FullSystemCheck()
        {
            var result = await _systemStatusService.IsFullSystemActive();
            var response = StatusCode(Convert.ToInt32(HttpStatusCodeEnum.Unknown));

            if (result)
            {
                response = StatusCode(Convert.ToInt32(HttpStatusCodeEnum.Success));
            }
            else
            {
                response = StatusCode(Convert.ToInt32(HttpStatusCodeEnum.SystemUnavailable));
            }
            
            return response;

        }
    }
}

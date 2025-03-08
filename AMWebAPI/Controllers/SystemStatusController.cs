using AMWebAPI.Services.CoreServices;
using AMWebAPI.Tools;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
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
            _logger.LogInfo("+");
            var result = await _systemStatusService.FullSystemCheck();
            _logger.LogInfo("-");

            return new ObjectResult(result);

        }
    }
}

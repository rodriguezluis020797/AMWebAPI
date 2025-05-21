using AMData.Models;
using AMServices.CoreServices;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class SystemStatusController(ISystemStatusService systemStatusService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> FullSystemCheck()
    {
        var result = await systemStatusService.IsFullSystemActive();
        
        return StatusCode(Convert.ToInt32( await systemStatusService.IsFullSystemActive() ? HttpStatusCodeEnum.Success : HttpStatusCodeEnum.ServerError),
            result);
    }
}
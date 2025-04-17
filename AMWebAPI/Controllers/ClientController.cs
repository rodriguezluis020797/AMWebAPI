using AMData.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class ClientController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return StatusCode((int)HttpStatusCodeEnum.Success);
        }
    }
}

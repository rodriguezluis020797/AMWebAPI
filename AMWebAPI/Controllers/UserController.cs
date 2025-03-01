using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class UserController : Controller
    {
        public UserController()
        {
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(){
            return new ObjectResult(StatusCode(200));
        }
    }
}

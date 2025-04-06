using AMData.Models;
using AMTools;
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
        private readonly IConfiguration _configuration;
        public IdentityController(IAMLogger logger, IIdentityService identityService, IConfiguration configuration)
        {
            _logger = logger;
            _identityService = identityService;
            _configuration = configuration;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LogIn([FromBody] UserDTO dto)
        {
            _logger.LogInfo("+");
            var response = new UserDTO();
            var jwToken = string.Empty;
            var refreshToken = string.Empty;
            try
            {

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = "0.0.0.0";
                }

                response = _identityService.LogIn(dto, ipAddress, out jwToken, out refreshToken);
            }
            catch (ArgumentException)
            {
                return StatusCode(400);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(500);
            }

            Response.Cookies.Append(SessionClaimEnum.JWToken.ToString(), jwToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(Convert.ToInt32(_configuration["CookieSettings:CookieExperationDays"]!))
            });

            Response.Cookies.Append(SessionClaimEnum.RefreshToken.ToString(), refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(Convert.ToInt32(_configuration["CookieSettings:CookieExperationDays"]!))
            });

            _logger.LogInfo("-");
            return StatusCode(200, dto);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Ping()
        {
            var jwToken = Request.Cookies["JWToken"];
            var refreshToken = Request.Cookies[SessionClaimEnum.RefreshToken.ToString()];
            var newJWT = string.Empty;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {

                if (string.IsNullOrEmpty(jwToken))
                {
                    return StatusCode(200);
                }

                if (IdentityTool.IsTheJWTExpired(jwToken) || string.IsNullOrEmpty(refreshToken))
                {
                    if (string.IsNullOrEmpty(refreshToken))
                    {
                        return StatusCode(400);
                    }

                    if (string.IsNullOrEmpty(ipAddress))
                    {
                        ipAddress = "0.0.0.0";
                    }

                    newJWT = _identityService.RefreshJWToken(jwToken, refreshToken, ipAddress);
                }
                else
                {
                    return StatusCode(200);
                }
            }
            catch (ArgumentException)
            {
                return StatusCode(400);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(500);
            }

            Response.Cookies.Append(SessionClaimEnum.JWToken.ToString(), newJWT, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(Convert.ToInt32(_configuration["CookieSettings:CookieExperationDays"]!))
            });

            Response.Cookies.Append(SessionClaimEnum.RefreshToken.ToString(), refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(Convert.ToInt32(_configuration["CookieSettings:CookieExperationDays"]!))
            });

            return StatusCode(200);
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
            catch (UnauthorizedAccessException e)
            {
                _logger.LogError(e.ToString());
                return Unauthorized();
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                response = new UserDTO();
                return StatusCode(500);
            }

            return new OkObjectResult(response);
        }
    }
}
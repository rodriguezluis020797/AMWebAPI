using AMData.Models;
using AMData.Models.DTOModels;
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
                var fingerprint = new FingerprintDTO()
                {
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Language = HttpContext.Request.Headers["X-Fingerprint-Language"].ToString(),
                    Platform = HttpContext.Request.Headers["X-Fingerprint-Platform"].ToString(),
                    UserAgent = HttpContext.Request.Headers["X-Fingerprint-UA"].ToString()
                };
                fingerprint.Validate();

                response = _identityService.LogIn(dto, fingerprint, out jwToken, out refreshToken);
            }
            catch (ArgumentException)
            {
                return StatusCode(Convert.ToInt32(HttpStatusCodeEnum.BadCredentials), dto);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(Convert.ToInt32(HttpStatusCodeEnum.ServerError));
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
            return StatusCode(Convert.ToInt32(HttpStatusCodeEnum.Success), dto);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Ping()
        {
            var jwToken = Request.Cookies["JWToken"];
            var refreshToken = Request.Cookies[SessionClaimEnum.RefreshToken.ToString()];
            var newJWT = string.Empty;
            var response = StatusCode(Convert.ToInt32(HttpStatusCodeEnum.Unknown));

            try
            {
                var fingerprint = new FingerprintDTO()
                {
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Language = HttpContext.Request.Headers["X-Fingerprint-Language"].ToString(),
                    Platform = HttpContext.Request.Headers["X-Fingerprint-Platform"].ToString(),
                    UserAgent = HttpContext.Request.Headers["X-Fingerprint-UA"].ToString()
                };
                fingerprint.Validate();

                if (!string.IsNullOrEmpty(jwToken) || !string.IsNullOrEmpty(refreshToken))
                {
                    if (IdentityTool.IsTheJWTExpired(jwToken))
                    {

                        newJWT = _identityService.RefreshJWToken(jwToken, refreshToken, fingerprint);

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
                    }
                }
                response = StatusCode(Convert.ToInt32(HttpStatusCodeEnum.Success));
            }
            catch (ArgumentException)
            {
                response = StatusCode(Convert.ToInt32(HttpStatusCodeEnum.BadCredentials));
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                response = StatusCode(Convert.ToInt32(HttpStatusCodeEnum.ServerError));
            }

            return response;
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
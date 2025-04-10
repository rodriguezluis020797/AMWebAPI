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

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(Convert.ToInt32(_configuration["CookieSettings:CookieExperationDays"]!))
            };

            Response.Cookies.Append(SessionClaimEnum.JWToken.ToString(), jwToken, cookieOptions);

            Response.Cookies.Append(SessionClaimEnum.RefreshToken.ToString(), refreshToken, cookieOptions);

            _logger.LogInfo("-");
            return StatusCode(Convert.ToInt32(HttpStatusCodeEnum.Success), dto);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LogOut()
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(-1)
            };

            Response.Cookies.Append(SessionClaimEnum.JWToken.ToString(), string.Empty, cookieOptions);
            Response.Cookies.Append(SessionClaimEnum.RefreshToken.ToString(), string.Empty, cookieOptions);
            Response.Cookies.Delete(SessionClaimEnum.JWToken.ToString(), cookieOptions);
            Response.Cookies.Delete(SessionClaimEnum.RefreshToken.ToString(), cookieOptions);

            return StatusCode(Convert.ToInt32(HttpStatusCodeEnum.Success), true);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Ping()
        {
            var jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
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

                        var cookieOptions = new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.None,
                            Expires = DateTime.UtcNow.AddDays(Convert.ToInt32(_configuration["CookieSettings:CookieExperationDays"]!))
                        };

                        Response.Cookies.Append(SessionClaimEnum.JWToken.ToString(), newJWT, cookieOptions);

                        Response.Cookies.Append(SessionClaimEnum.RefreshToken.ToString(), refreshToken, cookieOptions);
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
            var dtoResponse = new UserDTO();
            var response = StatusCode(Convert.ToInt32(HttpStatusCodeEnum.Unknown), dtoResponse);
            var jwToken = string.Empty;

            try
            {
                if (!IdentityTool.IsValidPassword(dto.Password))
                {
                    response = StatusCode(Convert.ToInt32(HttpStatusCodeEnum.BadPassword), null);
                    return response;
                }

                jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

                if (!string.IsNullOrEmpty(jwToken))
                {
                    dtoResponse = _identityService.UpdatePassword(dto, jwToken);
                    response = StatusCode(Convert.ToInt32(HttpStatusCodeEnum.Unknown), dtoResponse);
                }
                else
                {
                    throw new Exception(nameof(jwToken));
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                dtoResponse = new UserDTO();
                response = StatusCode(Convert.ToInt32(HttpStatusCodeEnum.Unknown), dtoResponse);
            }

            return response;
        }
    }
}
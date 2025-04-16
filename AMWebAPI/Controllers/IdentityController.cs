using AMData.Models;
using AMData.Models.DTOModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.IdentityServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static AMWebAPI.Services.IdentityServices.IdentityService;

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

            try
            {
                var fingerprint = ExtractFingerprintFromHeaders();
                fingerprint.Validate();

                var loginResult = await _identityService.LogInAsync(dto, fingerprint);
                response = loginResult.userDTO;

                SetAuthCookies(loginResult.jwToken, loginResult.refreshToken);
            }
            catch (ArgumentException)
            {
                return StatusCode((int)HttpStatusCodeEnum.BadCredentials, dto);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return StatusCode((int)HttpStatusCodeEnum.ServerError);
            }

            _logger.LogInfo("-");
            return StatusCode((int)HttpStatusCodeEnum.Success, dto);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult LogOut()
        {
            ExpireAuthCookies();
            return StatusCode((int)HttpStatusCodeEnum.Success, true);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Ping()
        {
            var jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            var refreshToken = Request.Cookies[SessionClaimEnum.RefreshToken.ToString()];
            var response = StatusCode((int)HttpStatusCodeEnum.Unknown);

            try
            {
                var fingerprint = ExtractFingerprintFromHeaders();
                fingerprint.Validate();

                if (!string.IsNullOrEmpty(jwToken) || !string.IsNullOrEmpty(refreshToken))
                {
                    if (IdentityTool.IsTheJWTExpired(jwToken))
                    {
                        var newJWT = await _identityService.RefreshJWToken(jwToken, refreshToken, fingerprint);
                        SetAuthCookies(newJWT, refreshToken);
                    }
                }

                response = StatusCode((int)HttpStatusCodeEnum.Success);
            }
            catch (ArgumentException)
            {
                response = StatusCode((int)HttpStatusCodeEnum.BadCredentials);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                response = StatusCode((int)HttpStatusCodeEnum.ServerError);
            }

            return response;
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] UserDTO dto)
        {
            _logger.LogInfo("+");
            var dtoResponse = new UserDTO();
            var response = StatusCode((int)HttpStatusCodeEnum.Unknown, dtoResponse);

            try
            {
                var jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

                if (string.IsNullOrEmpty(jwToken))
                    throw new Exception(nameof(jwToken));

                dtoResponse = await _identityService.UpdatePasswordAsync(dto, jwToken);
                response = StatusCode((int)HttpStatusCodeEnum.Unknown, dtoResponse);
            }
            catch (ArgumentException)
            {
                response = StatusCode((int)HttpStatusCodeEnum.BadPassword, new UserDTO());
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                response = StatusCode((int)HttpStatusCodeEnum.ServerError, new UserDTO());
            }

            return response;
        }

        private FingerprintDTO ExtractFingerprintFromHeaders()
        {
            return new FingerprintDTO
            {
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Language = HttpContext.Request.Headers["X-Fingerprint-Language"].ToString(),
                Platform = HttpContext.Request.Headers["X-Fingerprint-Platform"].ToString(),
                UserAgent = HttpContext.Request.Headers["X-Fingerprint-UA"].ToString()
            };
        }

        private void SetAuthCookies(string jwt, string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(Convert.ToInt32(_configuration["CookieSettings:CookieExperationDays"]!))
            };

            Response.Cookies.Append(SessionClaimEnum.JWToken.ToString(), jwt, cookieOptions);
            Response.Cookies.Append(SessionClaimEnum.RefreshToken.ToString(), refreshToken, cookieOptions);
        }

        private void ExpireAuthCookies()
        {
            var expiredOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(-1)
            };

            Response.Cookies.Append(SessionClaimEnum.JWToken.ToString(), string.Empty, expiredOptions);
            Response.Cookies.Append(SessionClaimEnum.RefreshToken.ToString(), string.Empty, expiredOptions);
            Response.Cookies.Delete(SessionClaimEnum.JWToken.ToString(), expiredOptions);
            Response.Cookies.Delete(SessionClaimEnum.RefreshToken.ToString(), expiredOptions);
        }
    }
}

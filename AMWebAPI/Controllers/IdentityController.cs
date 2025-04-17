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
    public class IdentityController : ControllerBase
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
        public async Task<IActionResult> LogIn([FromBody] ProvidderDTO dto)
        {
            _logger.LogInfo("+");

            try
            {
                var fingerprint = ExtractFingerprint();
                fingerprint.Validate();

                var loginResult = await _identityService.LogInAsync(dto, fingerprint);
                SetAuthCookies(loginResult.jwToken, loginResult.refreshToken);

                _logger.LogInfo("-");
                return StatusCode((int)HttpStatusCodeEnum.Success, loginResult.userDTO);
            }
            catch (ArgumentException)
            {
                return StatusCode((int)HttpStatusCodeEnum.BadCredentials, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return StatusCode((int)HttpStatusCodeEnum.ServerError, dto);
            }
            finally
            {
                _logger.LogInfo("-");
            }
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
            try
            {
                var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
                var refresh = Request.Cookies[SessionClaimEnum.RefreshToken.ToString()];
                var fingerprint = ExtractFingerprint();

                fingerprint.Validate();

                if (!string.IsNullOrEmpty(jwt) || !string.IsNullOrEmpty(refresh))
                {
                    if (IdentityTool.IsTheJWTExpired(jwt))
                    {
                        var newJwt = await _identityService.RefreshJWToken(jwt, refresh, fingerprint);
                        SetAuthCookies(newJwt, refresh);
                    }
                }

                return StatusCode((int)HttpStatusCodeEnum.Success);
            }
            catch (ArgumentException)
            {
                return StatusCode((int)HttpStatusCodeEnum.BadCredentials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return StatusCode((int)HttpStatusCodeEnum.ServerError);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ProvidderDTO dto)
        {
            _logger.LogInfo("+");

            try
            {
                var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
                if (string.IsNullOrWhiteSpace(jwt)) throw new Exception("JWT token missing.");

                var result = await _identityService.UpdatePasswordAsync(dto, jwt);
                return StatusCode((int)HttpStatusCodeEnum.Success, result);
            }
            catch (ArgumentException)
            {
                return StatusCode((int)HttpStatusCodeEnum.BadPassword, new ProvidderDTO());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return StatusCode((int)HttpStatusCodeEnum.ServerError, new ProvidderDTO());
            }
            finally
            {
                _logger.LogInfo("-");
            }
        }

        private FingerprintDTO ExtractFingerprint() => new()
        {
            IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Language = Request.Headers["X-Fingerprint-Language"],
            Platform = Request.Headers["X-Fingerprint-Platform"],
            UserAgent = Request.Headers["X-Fingerprint-UA"]
        };

        private void SetAuthCookies(string jwt, string refreshToken)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(int.Parse(_configuration["CookieSettings:CookieExperationDays"]!))
            };

            Response.Cookies.Append(SessionClaimEnum.JWToken.ToString(), jwt, options);
            Response.Cookies.Append(SessionClaimEnum.RefreshToken.ToString(), refreshToken, options);
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

            var jwtKey = SessionClaimEnum.JWToken.ToString();
            var refreshKey = SessionClaimEnum.RefreshToken.ToString();

            Response.Cookies.Append(jwtKey, string.Empty, expiredOptions);
            Response.Cookies.Append(refreshKey, string.Empty, expiredOptions);
            Response.Cookies.Delete(jwtKey, expiredOptions);
            Response.Cookies.Delete(refreshKey, expiredOptions);
        }
    }
}
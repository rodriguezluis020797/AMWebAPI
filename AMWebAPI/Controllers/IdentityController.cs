using AMData.Models;
using AMData.Models.DTOModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Services.IdentityServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class IdentityController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IAMLogger _logger;
        private readonly IIdentityService _identityService;

        public IdentityController(IAMLogger logger, IIdentityService identityService, IConfiguration configuration)
        {
            _logger = logger;
            _identityService = identityService;
            _configuration = configuration;
        }

        [HttpGet]
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

        private FingerprintDTO ExtractFingerprint() => new()
        {
            IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Language = Request.Headers["X-Fingerprint-Language"],
            Platform = Request.Headers["X-Fingerprint-Platform"],
            UserAgent = Request.Headers["X-Fingerprint-UA"]
        };

        [HttpGet]
        public async Task<IActionResult> IsLoggedIn()
        {
            return StatusCode((int)HttpStatusCodeEnum.Success, true);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LogIn([FromBody] ProviderDTO dto)
        {
            _logger.LogInfo("+");
            var response = new BaseDTO();

            try
            {
                var fingerprint = ExtractFingerprint();
                fingerprint.Validate();

                var loginResult = await _identityService.LogInAsync(dto, fingerprint);
                response = loginResult.baseDTO;
                SetAuthCookies(loginResult.jwToken, loginResult.refreshToken);

                _logger.LogInfo("-");
                return StatusCode((int)HttpStatusCodeEnum.Success, response);
            }
            catch (ArgumentException)
            {
                response.ErrorMessage = "Invalid Credentials.";
                return StatusCode((int)HttpStatusCodeEnum.Success, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return StatusCode((int)HttpStatusCodeEnum.ServerError, response);
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
                    return StatusCode((int)HttpStatusCodeEnum.LoggedIn);
                }
                return StatusCode((int)HttpStatusCodeEnum.Success);
            }
            catch (ArgumentException)
            {
                return StatusCode((int)HttpStatusCodeEnum.Unauthorized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return StatusCode((int)HttpStatusCodeEnum.ServerError);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ProviderDTO dto)
        {
            _logger.LogInfo("+");

            try
            {
                await _identityService.ResetPasswordAsync(dto);

                return StatusCode((int)HttpStatusCodeEnum.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return StatusCode((int)HttpStatusCodeEnum.ServerError);
            }
            finally
            {
                _logger.LogInfo("-");
            }
        }

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

        [HttpPost]
        public async Task<IActionResult> UpdatePassword([FromBody] ProviderDTO dto)
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
                return StatusCode((int)HttpStatusCodeEnum.BadPassword, new ProviderDTO());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return StatusCode((int)HttpStatusCodeEnum.ServerError, new ProviderDTO());
            }
            finally
            {
                _logger.LogInfo("-");
            }
        }
    }
}
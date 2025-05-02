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
        [AllowAnonymous]
        public async Task<IActionResult> IsLoggedIn()
        {
            var fingerprint = ExtractFingerprint();

            try
            {
                var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
                var refreshToken = Request.Cookies[SessionClaimEnum.RefreshToken.ToString()];
                fingerprint.Validate();

                if (!string.IsNullOrEmpty(jwt) || !string.IsNullOrEmpty(refreshToken))
                {
                    var newJwt = await _identityService.RefreshJWToken(jwt, refreshToken, fingerprint);
                    SetAuthCookies(newJwt, refreshToken);
                    return StatusCode((int)HttpStatusCodeEnum.LoggedIn);
                }
                else
                {
                    return StatusCode((int)HttpStatusCodeEnum.NotLoggedIn);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogError($"IP Address: {fingerprint.IPAddress} - {ex.ToString()}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

            ExpireAuthCookies();
            return StatusCode((int)HttpStatusCodeEnum.NotLoggedIn);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LogIn([FromBody] ProviderDTO dto)
        {
            _logger.LogInfo("+");
            var response = new ProviderDTO();

            try
            {
                var fingerprint = ExtractFingerprint();
                fingerprint.Validate();

                var loginResult = await _identityService.LogInAsync(dto, fingerprint);
                response = loginResult.providerDTO;
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
                return StatusCode((int)HttpStatusCodeEnum.ServerError);
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
            var fingerprint = ExtractFingerprint();

            try
            {
                var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
                var refreshToken = Request.Cookies[SessionClaimEnum.RefreshToken.ToString()];
                fingerprint.Validate();

                if (!string.IsNullOrEmpty(jwt) || !string.IsNullOrEmpty(refreshToken))
                {
                    if (IdentityTool.IsTheJWTExpired(jwt))
                    {
                        var newJwt = await _identityService.RefreshJWToken(jwt, refreshToken, fingerprint);
                        SetAuthCookies(newJwt, refreshToken);
                    }
                }
                return StatusCode((int)HttpStatusCodeEnum.Success);
            }
            catch (ArgumentException ae)
            {
                _logger.LogError($"IP Address: {fingerprint.IPAddress} - {ae.ToString()}");
                ExpireAuthCookies();
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
            var result = new BaseDTO();

            try
            {
                var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
                if (string.IsNullOrWhiteSpace(jwt)) throw new Exception("JWT token missing.");

                result = await _identityService.UpdatePasswordAsync(dto, jwt);
                return StatusCode((int)HttpStatusCodeEnum.Success, result);
            }
            catch (ArgumentException)
            {
                result.ErrorMessage = "Bad password or current password doesn't match";
                return StatusCode((int)HttpStatusCodeEnum.Success, result);
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
    }
}
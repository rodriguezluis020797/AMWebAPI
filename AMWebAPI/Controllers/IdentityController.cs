using AMData.Models;
using AMData.Models.DTOModels;
using AMServices.IdentityServices;
using AMTools;
using AMTools.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Authorize]
public class IdentityController(IAMLogger logger, IIdentityService identityService, IConfiguration configuration)
    : ControllerBase
{
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

    private FingerprintDTO ExtractFingerprint()
    {
        return new FingerprintDTO
        {
            IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Language = Request.Headers["X-Fingerprint-Language"],
            Platform = Request.Headers["X-Fingerprint-Platform"],
            UserAgent = Request.Headers["X-Fingerprint-UA"]
        };
    }

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
                var newJwt = await identityService.RefreshJWT(jwt, refreshToken, fingerprint);
                SetAuthCookies(newJwt, refreshToken);
                return StatusCode((int)HttpStatusCodeEnum.LoggedIn);
            }

            return StatusCode((int)HttpStatusCodeEnum.NotLoggedIn);
        }
        catch (ArgumentException ex)
        {
            logger.LogError($"IP Address: {fingerprint.IPAddress} - {ex}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
        }

        ExpireAuthCookies();
        return StatusCode((int)HttpStatusCodeEnum.NotLoggedIn);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> LogIn([FromBody] ProviderDTO dto)
    {
        logger.LogInfo("+");
        var response = new ProviderDTO();

        try
        {
            var fingerprint = ExtractFingerprint();
            fingerprint.Validate();

            var loginResult = await identityService.LogInAsync(dto, fingerprint);
            response = loginResult.ProviderDto;
            SetAuthCookies(loginResult.Jwt, loginResult.RefreshToken);

            logger.LogInfo("-");
            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (ArgumentException)
        {
            response.ErrorMessage = "Invalid Credentials.";
            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
        finally
        {
            logger.LogInfo("-");
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
                if (IdentityTool.IsTheJWTExpired(jwt))
                {
                    var newJwt = await identityService.RefreshJWT(jwt, refreshToken, fingerprint);
                    SetAuthCookies(newJwt, refreshToken);
                }

            return StatusCode((int)HttpStatusCodeEnum.Success);
        }
        catch (ArgumentException ae)
        {
            logger.LogError($"IP Address: {fingerprint.IPAddress} - {ae}");
            ExpireAuthCookies();
            return StatusCode((int)HttpStatusCodeEnum.Unauthorized);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ProviderDTO dto)
    {
        try
        {
            await identityService.ResetPasswordAsync(dto);

            return StatusCode((int)HttpStatusCodeEnum.Success);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
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
            Expires = DateTime.UtcNow.AddDays(int.Parse(configuration["CookieSettings:CookieExperationDays"]!))
        };

        Response.Cookies.Append(SessionClaimEnum.JWToken.ToString(), jwt, options);
        Response.Cookies.Append(SessionClaimEnum.RefreshToken.ToString(), refreshToken, options);
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePassword([FromBody] ProviderDTO dto)
    {
        logger.LogInfo("+");
        var result = new BaseDTO();

        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwt)) throw new Exception("JWT token missing.");

            result = await identityService.UpdatePasswordAsync(dto, jwt);
            return StatusCode((int)HttpStatusCodeEnum.Success, result);
        }
        catch (ArgumentException)
        {
            result.ErrorMessage = "Bad password or current password doesn't match";
            return StatusCode((int)HttpStatusCodeEnum.Success, result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
        finally
        {
            logger.LogInfo("-");
        }
    }
}
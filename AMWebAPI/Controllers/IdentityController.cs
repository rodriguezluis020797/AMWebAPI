using AMData.Models;
using AMTools.Tools;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.IdentityServices;
using Azure;
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
        public IdentityController(IAMLogger logger, IIdentityService identityService)
        {
            _logger = logger;
            _identityService = identityService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LogIn([FromBody] UserDTO dto)
        {
            _logger.LogInfo("+");
            var response = new UserDTO();
            try
            {
                
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = "0.0.0.0";
                }

                response = _identityService.LogIn(dto, ipAddress);
                response.EMail = dto.EMail;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                response = new UserDTO();
                response.ErrorMessage = "Server Error.";
                response.RequestStatus = RequestStatusEnum.Error;
            }

            Response.Cookies.Append(SessionClaimEnum.JWT.ToString(), response.JWTToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            response.JWTToken = string.Empty;
            _logger.LogInfo("-");
            return new ObjectResult(response);
        }

        [HttpPost]
        public async Task<IActionResult> RefreshToken([FromBody] UserDTO dto)
        {
            _logger.LogInfo("+");
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = "0.0.0.0";
                }
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);

                dto.JWTToken = _identityService.RefreshToken(token, dto.RefreshToken, ipAddress);
                dto.RequestStatus = RequestStatusEnum.Success;
            }
            catch (UnauthorizedAccessException e)
            {
                _logger.LogError(e.ToString());
                dto = new UserDTO();
                dto.ErrorMessage = "Server Error.";
                dto.RequestStatus = RequestStatusEnum.JWTError;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                dto = new UserDTO();
                dto.ErrorMessage = "Server Error.";
                dto.RequestStatus = RequestStatusEnum.Error;
            }

            Response.Cookies.Append(SessionClaimEnum.JWT.ToString(), dto.JWTToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            dto.JWTToken = string.Empty;

            return new ObjectResult(dto);
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
                response = new UserDTO();
                response.ErrorMessage = "Server Error.";
                response.RequestStatus = RequestStatusEnum.JWTError;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                response = new UserDTO();
                response.ErrorMessage = "Server Error.";
                response.RequestStatus = RequestStatusEnum.Error;
            }

            return new OkObjectResult(response);
        }
    }
}
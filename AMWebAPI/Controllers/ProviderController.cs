using AMData.Models;
using AMTools.Tools;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.CoreServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class ProviderController : ControllerBase
    {
        private readonly IProviderService _providerService;
        private readonly IAMLogger _logger;

        public ProviderController(IAMLogger logger, IProviderService providerService)
        {
            _logger = logger;
            _providerService = providerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProvider()
        {
            _logger.LogInfo("+");

            try
            {
                var jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
                if (string.IsNullOrWhiteSpace(jwToken))
                    throw new Exception("JWT token missing from cookies.");

                var provider = await _providerService.GetProviderAsync(jwToken);
                return StatusCode((int)HttpStatusCodeEnum.Success, provider);
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

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateProvider([FromBody] ProviderDTO dto)
        {
            _logger.LogInfo("+");
            try
            {
                var result = await _providerService.CreateProviderAsync(dto);
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

        [HttpPost]
        public async Task<IActionResult> UpdateProvider([FromBody] ProviderDTO dto)
        {
            _logger.LogInfo("+");
            try
            {
                var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

                var response = await _providerService.UpdateProviderAsync(dto, jwt);

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
    }
}
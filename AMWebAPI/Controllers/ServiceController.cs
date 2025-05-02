using AMData.Models;
using AMData.Models.DTOModels;
using AMTools.Tools;
using AMWebAPI.Services.CoreServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Authorize]
public class ServiceController : ControllerBase
{
    private readonly IAMLogger _logger;
    private readonly IServiceService _serviceService;

    public ServiceController(IAMLogger logger, IServiceService serviceService)
    {
        _logger = logger;
        _serviceService = serviceService;
    }

    [HttpPost]
    public async Task<ActionResult> CreateService([FromBody] ServiceDTO dto)
    {
        _logger.LogInfo("+");
        try
        {
            var jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwToken))
            {
                throw new Exception("JWT token missing from cookies.");
            }
            var result = await _serviceService.CreateServiceAsync(dto, jwToken);
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

    [HttpGet]
    public async Task<ActionResult> GetServices()
    {
        _logger.LogInfo("+");
        try
        {
            var jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwToken))
            {
                throw new Exception("JWT token missing from cookies.");
            }
            var result = await _serviceService.GetServicesAsync(jwToken);
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
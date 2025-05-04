using AMData.Models;
using AMData.Models.DTOModels;
using AMServices.CoreServices;
using AMTools.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
[Authorize]
public class ServiceController(IAMLogger logger, IServiceService serviceService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> CreateService([FromBody] ServiceDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwToken)) throw new Exception("JWT token missing from cookies.");
            var result = await serviceService.CreateServiceAsync(dto, jwToken);
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

    [HttpGet]
    public async Task<ActionResult> GetServices()
    {
        logger.LogInfo("+");
        try
        {
            var jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwToken)) throw new Exception("JWT token missing from cookies.");
            var result = await serviceService.GetServicesAsync(jwToken);
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

    [HttpPost]
    public async Task<ActionResult> UpdateService([FromBody] ServiceDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwToken)) throw new Exception("JWT token missing from cookies.");
            var result = await serviceService.UpdateServiceAsync(dto, jwToken);
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

    [HttpPost]
    public async Task<ActionResult> DeleteService([FromBody] ServiceDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwToken)) throw new Exception("JWT token missing from cookies.");
            var result = await serviceService.DeleteServiceAsync(dto, jwToken);
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
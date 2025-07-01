using AMData.Models;
using AMData.Models.DTOModels;
using AMServices.CoreServices;
using AMTools;
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
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var result = await serviceService.CreateServiceAsync(dto, jwt);
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
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];

            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");
            
            var result = await serviceService.GetServicesAsync(jwt);
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
    public async Task<ActionResult> GetServicePrice([FromBody] ServiceDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var result = await serviceService.GetServicePrice(dto, jwt);

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
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var result = await serviceService.UpdateServiceAsync(dto, jwt);
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
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var result = await serviceService.DeleteServiceAsync(dto, jwt);
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
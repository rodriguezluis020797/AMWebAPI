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
public class ProviderController(IAMLogger logger, IProviderService providerService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    //Finazlized...
    public async Task<IActionResult> CreateProvider([FromBody] ProviderDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var result = await providerService.CreateProviderAsync(dto);
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
    public async Task<IActionResult> GetProvider()
    {
        logger.LogInfo("+");

        try
        {
            var jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwToken))
                throw new Exception("JWT token missing from cookies.");

            var provider = await providerService.GetProviderAsync(jwToken);
            return StatusCode((int)HttpStatusCodeEnum.Success, provider);
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
    public async Task<IActionResult> UpdateEMail([FromBody] ProviderDTO dto)
    {
        logger.LogInfo("+");
        var response = new ProviderDTO();
        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

            response = await providerService.UpdateEMailAsync(dto, jwt);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (ArgumentException)
        {
            return StatusCode((int)HttpStatusCodeEnum.BadCredentials, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError, response);
        }
        finally
        {
            logger.LogInfo("-");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProvider([FromBody] ProviderDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

            var response = await providerService.UpdateProviderAsync(dto, jwt);

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
    public async Task<IActionResult> UpdateEMail([FromQuery] string guid)
    {
        logger.LogInfo("+");
        var response = new BaseDTO();
        try
        {
            //response = await providerService.UpdateEMailAsync(guid);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError, response);
        }
        finally
        {
            logger.LogInfo("-");
        }
    }
    
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEMail([FromQuery] string guid, [FromQuery] bool verifying)
    {
        logger.LogInfo("+");
        var response = new BaseDTO();
        try
        {
            response = await providerService.VerifyEMailAsync(guid, verifying);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError, response);
        }
        finally
        {
            logger.LogInfo("-");
        }
    }
}
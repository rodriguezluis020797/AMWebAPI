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
public class ProviderController(IAMLogger logger, IProviderService providerService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
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

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> GetProviderReviewForSubmission([FromBody] ProviderReviewDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var result = await providerService.GetProviderReviewForSubmissionAsync(dto);

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
    public async Task<IActionResult> GetProviderReviewsForProvider()
    {
        logger.LogInfo("+");
        try
        {
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");
            
            var result = await providerService.GetProviderReviewsForProviderAsync(jwt);
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
    [AllowAnonymous]
    public async Task<IActionResult> UpdateProviderReview([FromBody] ProviderReviewDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var result = await providerService.UpdateProviderReviewAsync(dto);
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
    public async Task<IActionResult> GetProvider([FromQuery] bool generateUrl)
    {
        logger.LogInfo("+");

        try
        {
            var jwToken = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            
            if (string.IsNullOrWhiteSpace(jwToken))
                throw new Exception("JWT token missing from cookies.");

            var provider = await providerService.GetProviderAsync(jwToken, generateUrl);
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

    [HttpGet]
    public async Task<IActionResult> GetProviderAlerts()
    {
        logger.LogInfo("+");

        try
        {
            var jwToken = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            if (string.IsNullOrWhiteSpace(jwToken))
                throw new Exception("JWT token missing from cookies.");

            var response = await providerService.GetProviderAlertsAsync(jwToken);
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

    [HttpPost]
    public async Task<IActionResult> AcknowledgeProviderAlert([FromBody] ProviderAlertDTO dto)
    {
        logger.LogInfo("+");

        try
        {
            var jwToken = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            if (string.IsNullOrWhiteSpace(jwToken))
                throw new Exception("JWT token missing from cookies.");

            var response = await providerService.AcknowledgeProviderAlertAsync(dto);
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

    [HttpPost]
    public async Task<IActionResult> UpdateEMail([FromBody] ProviderDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            
            
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var response = await providerService.UpdateEMailAsync(dto, jwt);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (ArgumentException)
        {
            return StatusCode((int)HttpStatusCodeEnum.BadCredentials);
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
    public async Task<IActionResult> UpdateProvider([FromBody] ProviderDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");
            
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
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEMail([FromQuery] string guid, [FromQuery] bool verifying)
    {
        logger.LogInfo("+");
        try
        {
            var response = await providerService.VerifyEMailAsync(guid, verifying);

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
    public async Task<IActionResult> CancelSubscription()
    {
        logger.LogInfo("+");
        try
        {
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            
            
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var response = await providerService.CancelSubscriptionAsync(jwt);

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
    public async Task<IActionResult> ReActivateSubscription()
    {
        logger.LogInfo("+");
        try
        {
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            
            
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var response = await providerService.ReActivateSubscriptionAsync(jwt);

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
    public async Task<IActionResult> GetProviderPublicView([FromQuery] string guid)
    {
        logger.LogInfo("+");
        try
        {
            var response = await providerService.GetProviderPublicViewAsync(guid);

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
}
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
public class ClientController(IAMLogger logger, IClientService clientService)
    : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");
            
            var response = await clientService.CreateClient(dto, jwt);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
        finally
        {
            logger.LogInfo("-");
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteClient([FromBody] ClientDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");
            
            var response = await clientService.DeleteClient(dto, jwt);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
        finally
        {
            logger.LogInfo("-");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetClients()
    {
        logger.LogInfo("+");
        try
        {
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];

            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");
            
            var response = await clientService.GetClients(jwt);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
        finally
        {
            logger.LogInfo("-");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateClient([FromBody] ClientDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");
            
            var response = await clientService.UpdateClient(dto, jwt);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
        finally
        {
            logger.LogInfo("-");
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetClientNotes([FromBody] ClientDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var jwt = Request.Cookies[nameof(SessionClaimEnum.JWToken)];
            
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var response = await clientService.GetClientNotes(dto, jwt);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
        finally
        {
            logger.LogInfo("-");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateClientNote([FromBody] ClientNoteDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var response = await clientService.CreateClientNote(dto);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
        finally
        {
            logger.LogInfo("-");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateClientNote([FromBody] ClientNoteDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var response = await clientService.UpdateClientNote(dto);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
        finally
        {
            logger.LogInfo("-");
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteClientNote([FromBody] ClientNoteDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var response = await clientService.DeleteClientNote(dto);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
        finally
        {
            logger.LogInfo("-");
        }
    }
}
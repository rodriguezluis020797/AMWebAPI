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
public class ClientController(IAMLogger logger, IConfiguration configuration, IClientService clientService)
    : ControllerBase
{
    private readonly IConfiguration _configuration = configuration;

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientDTO dto)
    {
        logger.LogInfo("+");
        var response = new ClientDTO();
        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

            response = await clientService.CreateClient(dto, jwt);

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
        var response = new ClientDTO();
        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

            response = await clientService.DeleteClient(dto, jwt);

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
        var response = new List<ClientDTO>();
        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

            response = await clientService.GetClients(jwt);

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
        var response = new ClientDTO();
        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

            response = await clientService.UpdateClient(dto, jwt);

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
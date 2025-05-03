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
public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly IConfiguration _configuration;
    private readonly IAMLogger _logger;

    public ClientController(IAMLogger logger, IConfiguration configuration, IClientService clientService)
    {
        _logger = logger;
        _configuration = configuration;
        _clientService = clientService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientDTO dto)
    {
        _logger.LogInfo("+");
        var response = new ClientDTO();
        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

            response = await _clientService.CreateClient(dto, jwt);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
        finally
        {
            _logger.LogInfo("-");
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteClient([FromBody] ClientDTO dto)
    {
        _logger.LogInfo("+");
        var response = new ClientDTO();
        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

            response = await _clientService.DeleteClient(dto, jwt);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
        finally
        {
            _logger.LogInfo("-");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetClients()
    {
        _logger.LogInfo("+");
        var response = new List<ClientDTO>();
        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

            response = await _clientService.GetClients(jwt);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
        finally
        {
            _logger.LogInfo("-");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateClient([FromBody] ClientDTO dto)
    {
        _logger.LogInfo("+");
        var response = new ClientDTO();
        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

            response = await _clientService.UpdateClient(dto, jwt);

            return StatusCode((int)HttpStatusCodeEnum.Success, response);
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            return StatusCode((int)HttpStatusCodeEnum.ServerError);
        }
        finally
        {
            _logger.LogInfo("-");
        }
    }
}
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
public class AppointmentController : ControllerBase
{
    private readonly IAMLogger _logger;
    private readonly IAppointmentService _appointmentService;

    public AppointmentController(IAMLogger logger, IAppointmentService appointmentService)
    {
        _logger = logger;
        _appointmentService = appointmentService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAppointments()
    {
        _logger.LogInfo("+");

        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var response = await _appointmentService.GetAllAppointmentsAsync(jwt);
            
            return StatusCode((int)HttpStatusCodeEnum.Success, response);
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
    public async Task<IActionResult> CreateAppointment([FromBody] AppointmentDTO dto)
    {
        _logger.LogInfo("+");

        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var result = await _appointmentService.CreateAppointmentAsync(dto, jwt);
            
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
    public async Task<IActionResult> UpdateAppointment([FromBody] AppointmentDTO dto)
    {
        _logger.LogInfo("+");

        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var result = await _appointmentService.UpdateAppointmentAsync(dto, jwt);
            
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
    public async Task<IActionResult> DeleteAppointment([FromBody] AppointmentDTO dto)
    {
        _logger.LogInfo("+");

        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var result = await _appointmentService.DeleteAppointmentAsync(dto, jwt);
            
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
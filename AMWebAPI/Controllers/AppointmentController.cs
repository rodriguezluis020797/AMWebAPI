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
public class AppointmentController(IAMLogger logger, IAppointmentService appointmentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAppointments()
    {
        logger.LogInfo("+");

        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var response = await appointmentService.GetAllAppointmentsAsync(jwt);

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
    public async Task<IActionResult> CreateAppointment([FromBody] AppointmentDTO dto)
    {
        logger.LogInfo("+");

        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var result = await appointmentService.CreateAppointmentAsync(dto, jwt);

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
    public async Task<IActionResult> UpdateAppointment([FromBody] AppointmentDTO dto)
    {
        logger.LogInfo("+");

        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var result = await appointmentService.UpdateAppointmentAsync(dto, jwt);

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
    public async Task<IActionResult> DeleteAppointment([FromBody] AppointmentDTO dto)
    {
        logger.LogInfo("+");

        try
        {
            var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];
            if (string.IsNullOrWhiteSpace(jwt))
                throw new Exception("JWT token missing from cookies.");

            var result = await appointmentService.DeleteAppointmentAsync(dto, jwt);

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
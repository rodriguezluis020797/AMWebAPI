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
public class MetricsController(IAMLogger logger, IMetricsService metricsService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GetMetricsByRange([FromBody] MetricsDTO dto)
    {
        logger.LogInfo("+");
        try
        {
            var jwToken = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

            var result = await metricsService.GetMetricsByRange(jwToken, dto);

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
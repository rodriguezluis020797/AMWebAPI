﻿using AMData.Models;
using AMData.Models.CoreModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Services.IdentityServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class ClientController : ControllerBase
    {
        private readonly IAMLogger _logger;
        private readonly IConfiguration _configuration;
        public ClientController(IAMLogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        [HttpGet]
        public async Task<IActionResult> GetClient()
        {
            try
            {
                var jwt = Request.Cookies[SessionClaimEnum.JWToken.ToString()];

                var principal = IdentityTool.GetClaimsFromJwt(jwt, _configuration["Jwt:Key"]!);

                var providerId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.ProviderId.ToString())?.Value);
                var clientModel = new ClientModel()
                {
                    ClientId = 1,
                    CreateDate = DateTime.UtcNow,
                    DeleteDate = null,
                    FirstName = "Test",
                    LastName = "Client",
                    MiddleName = null,
                    PhoneNumber = "1234567890",
                    ProviderId = providerId
                };

                return StatusCode((int)HttpStatusCodeEnum.Success, clientModel);
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
}

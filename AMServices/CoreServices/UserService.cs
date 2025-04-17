using AMData.Models;
using AMData.Models.CoreModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.DataServices;
using AMWebAPI.Services.IdentityServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMWebAPI.Services.CoreServices
{
    public interface IUserService
    {
        public Task<UserDTO> CreateUser(UserDTO dto);
        public Task<UserDTO> GetUser(string jwToken);
    }
    public class UserService : IUserService
    {
        private readonly IAMLogger _logger;
        private readonly AMCoreData _amCoreData;
        private readonly ICommunicationService _communicationService;
        private readonly IConfiguration _configuration;
        public UserService(IAMLogger logger, AMCoreData amCoreData, IIdentityService identityService, ICommunicationService communicationService, IConfiguration configuration)
        {
            _logger = logger;
            _amCoreData = amCoreData;
            _communicationService = communicationService;
            _configuration = configuration;
        }

        public async Task<UserDTO> CreateUser(UserDTO dto)
        {
            var response = new UserDTO();
            dto.Validate();
            if (!string.IsNullOrEmpty(dto.ErrorMessage))
            {
                return dto;
            }
            if (_amCoreData.Users.Any(x => x.EMail.Equals(dto.EMail)))
            {
                dto.ErrorMessage = $"User with given e-mail already exists.{Environment.NewLine}" +
                    $"Please wait to be given access.";
                return dto;
            }
            else
            {
                var user = new UserModel();
                user.CreateNewRecordFromDTO(dto);

                await _amCoreData.Users.AddAsync(user);
                await _amCoreData.SaveChangesAsync();

                var message = _configuration["Messages:NewUserMessage"];

                if (!string.IsNullOrEmpty(message))
                {
                    try
                    {
                        await _communicationService.AddUserCommunication(user.UserId, message);
                    }
                    catch
                    {
                        //do nothing... same message that would be sent is the same as displayed in UI.
                    }
                }
                _logger.LogAudit($"User Id: {user.UserId}{Environment.NewLine}E-Mail: {user.EMail}");

                dto.CreateNewRecordFromModel(user);
                return dto;
            }
        }

        public async Task<UserDTO> GetUser(string jwToken)
        {
            var response = new UserDTO();
            var principal = IdentityTool.GetClaimsFromJwt(jwToken, _configuration["Jwt:Key"]!);
            var userId = Convert.ToInt64(principal.FindFirst(SessionClaimEnum.UserId.ToString())?.Value);
            var sessionId = principal.FindFirst(SessionClaimEnum.SessionId.ToString())?.Value;

            var user = await _amCoreData.Users
                .Where(x => x.UserId == userId)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                throw new ArgumentException(nameof(userId));
            }
            else
            {
                response.CreateNewRecordFromModel(user);
                return response;
            }
        }
    }
}
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
        Task<UserDTO> CreateUser(UserDTO dto);
        Task<UserDTO> GetUser(string jwToken);
    }

    public class UserService : IUserService
    {
        private readonly IAMLogger _logger;
        private readonly AMCoreData _db;
        private readonly ICommunicationService _communicationService;
        private readonly IConfiguration _config;

        public UserService(
            IAMLogger logger,
            AMCoreData db,
            IIdentityService identityService, // NOTE: If unused, consider removing
            ICommunicationService communicationService,
            IConfiguration config)
        {
            _logger = logger;
            _db = db;
            _communicationService = communicationService;
            _config = config;
        }

        public async Task<UserDTO> CreateUser(UserDTO dto)
        {
            dto.Validate();
            if (!string.IsNullOrEmpty(dto.ErrorMessage))
                return dto;

            bool userExists = _db.Users.Any(x => x.EMail == dto.EMail);
            if (userExists)
            {
                dto.ErrorMessage = $"User with given e-mail already exists.{Environment.NewLine}" +
                                   $"Please wait to be given access.";
                return dto;
            }

            var user = new UserModel();
            user.CreateNewRecordFromDTO(dto);

            await _db.Users.AddAsync(user);
            await _db.SaveChangesAsync();

            await TrySendNewUserMessage(user.UserId);

            _logger.LogAudit($"User Id: {user.UserId}{Environment.NewLine}E-Mail: {user.EMail}");

            dto.CreateNewRecordFromModel(user);
            return dto;
        }

        public async Task<UserDTO> GetUser(string jwToken)
        {
            var claims = IdentityTool.GetClaimsFromJwt(jwToken, _config["Jwt:Key"]!);
            var userId = Convert.ToInt64(claims.FindFirst(SessionClaimEnum.UserId.ToString())?.Value);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                throw new ArgumentException(nameof(userId));

            var dto = new UserDTO();
            dto.CreateNewRecordFromModel(user);
            return dto;
        }

        private async Task TrySendNewUserMessage(long userId)
        {
            var message = _config["Messages:NewUserMessage"];
            if (string.IsNullOrWhiteSpace(message))
                return;

            try
            {
                await _communicationService.AddUserCommunication(userId, message);
            }
            catch
            {
                // Silent fail — UI will display message anyway
            }
        }
    }
}
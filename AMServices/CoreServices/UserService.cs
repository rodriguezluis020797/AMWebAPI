﻿using AMData.Models.CoreModels;
using AMTools.Tools;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.DataServices;
using AMWebAPI.Services.IdentityServices;
using Microsoft.Extensions.Configuration;

namespace AMWebAPI.Services.CoreServices
{
    public interface IUserService
    {
        public UserDTO CreateUser(UserDTO dto);
        public UserDTO GetUserById(string userId);
        public UserDTO GetUserByEMail(string eMail);
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

        public UserDTO CreateUser(UserDTO dto)
        {
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

                _amCoreData.Users.Add(user);
                _amCoreData.SaveChanges();

                var message = _configuration["Messages:NewUserMessage"];

                if (!string.IsNullOrEmpty(message))
                {
                    try
                    {
                        _communicationService.AddUserCommunication(user.UserId, message);
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

        public UserDTO GetUserByEMail(string eMail)
        {
            var dto = new UserDTO();

            var user = _amCoreData.Users
                .Where(x => x.EMail.Equals(eMail))
                .FirstOrDefault();

            if (user == null)
            {
                dto.ErrorMessage = "User Not Foound";
                return dto;
            }
            else
            {
                dto.CreateNewRecordFromModel(user);
            }
            return dto;
        }

        public UserDTO GetUserById(string userId)
        {
            var dto = new UserDTO();

            CryptographyTool.Decrypt(userId, out string decryptedId);

            long.TryParse(decryptedId, out long result);

            var user = _amCoreData.Users
                .Where(x => x.UserId == result)
                .FirstOrDefault();

            if (user == null)
            {
                throw new ArgumentException(nameof(userId));
            }
            else
            {
                dto.CreateNewRecordFromModel(user);
                return dto;
            }
        }
    }
}
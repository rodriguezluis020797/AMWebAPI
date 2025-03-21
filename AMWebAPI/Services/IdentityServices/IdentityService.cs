using AMWebAPI.Models.CoreModels;
using AMWebAPI.Models.DTOModels;
using AMWebAPI.Services.DataServices;
using AMWebAPI.Tools;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AMWebAPI.Services.IdentityServices
{
    public interface IIdentityService
    {
        public UserDTO LogIn(UserDTO dto, string ipAddress);
    }
    public class IdentityService : IIdentityService
    {
        private readonly IAMLogger _logger;
        private readonly AMCoreData _coreData;
        private readonly IConfiguration _configuration;
        public IdentityService(AMCoreData coreData, IAMLogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _coreData = coreData;
            _configuration = configuration;
        }
        public UserDTO LogIn(UserDTO dto, string ipAddress)
        {
            var user = _coreData.Users
                .Where(x => x.EMail.Equals(dto.EMail))
                .FirstOrDefault();

            if (user == null)
            {
                dto.Password = string.Empty;
                dto.RequestStatus = Models.RequestStatusEnum.BadRequest;
                return dto;
            }

            var decryptedPassword = string.Empty; //get after setting up identity server

            if (!dto.Password.Equals(decryptedPassword))
            {
                dto.Password = string.Empty;
                dto.RequestStatus = Models.RequestStatusEnum.BadRequest;
                return dto;
            }
            var session = new SessionModel()
            {
                CreateDate = DateTime.UtcNow,
                SessionId = 0,
                UserId = user.UserId
            };
            dto.CreateNewRecordFromModel(user);
            using (var trans = _coreData.Database.BeginTransaction())
            {
                _coreData.Sessions.Add(session);
                _coreData.SaveChanges();

                dto.JWTToken = GenerateJWTToken(dto.UserId, dto.EMail, session.ToString());

                dto.RequestStatus = Models.RequestStatusEnum.Success;
                _logger.LogAudit($"User Id: {user.UserId}{Environment.NewLine}" +
                    $"IP Address: {ipAddress}");

                trans.Commit();
            }
            return dto;
        }
        public string GenerateJWTToken(string userId, string email, string sessionId)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiresInMinutes"]));

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Sid, sessionId),
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expires,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
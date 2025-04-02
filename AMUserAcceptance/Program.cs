using AMData.Models.CoreModels;
using AMData.Models.IdentityModels;
using AMTools;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Transactions;

namespace AMUserAcceptance
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var logger = new AMDevLogger();

            logger.LogInfo("+");
            var programInstance = new Program();
            var config = new ConfigurationManager();
            config.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            await programInstance.SendUserCommunicationAsync(logger, config);

            logger.LogInfo("-");
        }

        private async Task SendUserCommunicationAsync(AMDevLogger logger, ConfigurationManager config)
        {
            var coreOptionsBuilder = new DbContextOptionsBuilder<AMCoreData>();
            coreOptionsBuilder.UseSqlServer(config.GetConnectionString("CoreConnectionString"));
            DbContextOptions<AMCoreData> coreOptions = coreOptionsBuilder.Options;

            var identityOptionsBuilder = new DbContextOptionsBuilder<AMIdentityData>();
            identityOptionsBuilder.UseSqlServer(config.GetConnectionString("IdentityConnectionString"));
            DbContextOptions<AMIdentityData> identityOptions = identityOptionsBuilder.Options;

            var users = new List<UserModel>();
            var passwordModel = new PasswordModel();
            var userComm = new UserCommunicationModel();
            var salt = string.Empty;
            var password = string.Empty;
            var _coreData = new AMCoreData(coreOptions, config);
            var _identityData = new AMIdentityData(identityOptions, config);

            users = _coreData.Users
                .Where(x => x.AccessGranted == false && x.DeleteDate == null && x.EMail.Equals(config["AcceptedUser"]))
                .OrderBy(x => x.CreateDate)
                .Take(5)
                .ToList();


            foreach (var user in users)
            {
                salt = IdentityTool.GenerateSaltString();
                password = IdentityTool.GenerateRandomPassword();
                passwordModel = new PasswordModel()
                {
                    CreateDate = DateTime.UtcNow,
                    DeleteDate = null,
                    HashedPassword = IdentityTool.HashPassword(password, salt),
                    PasswordId = 0,
                    Salt = salt,
                    Temporary = true,
                    UserId = user.UserId,
                };

                userComm = new UserCommunicationModel()
                {
                    UserId = user.UserId,
                    AttemptOne = null,
                    AttemptThree = null,
                    AttemptTwo = null,
                    CommunicationId = 0,
                    CreateDate = DateTime.UtcNow,
                    DeleteDate = null,
                    Message = $"Good news! You can now use the system!{Environment.NewLine}" +
                    $"Temporary password: {password}",
                    SendAfter = DateTime.UtcNow,
                    Sent = false
                };

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    user.AccessGranted = true;
                    _coreData.Users.Update(user);
                    _coreData.UserCommunications.Add(userComm);
                    _coreData.SaveChanges();

                    _identityData.Passwords.Add(passwordModel);
                    _identityData.SaveChanges();

                    scope.Complete();
                }
            }
        }
    }
}

using AMData.Models.CoreModels;
using AMData.Models.IdentityModels;
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
            var _coreData = new AMCoreData(coreOptions, config);
            var _identityData = new AMIdentityData(identityOptions, config);

            users = _coreData.Users
                .Where(x => x.AccessGranted == false && x.DeleteDate == null && x.EMail.Equals(config["AcceptedUser"]))
                .OrderBy(x => x.CreateDate)
                .Take(5)
                .ToList();

            foreach (var user in users)
            {
                passwordModel = new PasswordModel();
                userComm = new UserCommunicationModel();

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    user.AccessGranted = true;

                }
            }
        }
    }
}

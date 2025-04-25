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

            await programInstance.SendProviderCommunicationAsync(logger, config);

            logger.LogInfo("-");
        }

        private async Task SendProviderCommunicationAsync(AMDevLogger logger, ConfigurationManager config)
        {
            var coreOptionsBuilder = new DbContextOptionsBuilder<AMCoreData>();
            coreOptionsBuilder.UseSqlServer(config.GetConnectionString("CoreConnectionString"));
            DbContextOptions<AMCoreData> coreOptions = coreOptionsBuilder.Options;

            var identityOptionsBuilder = new DbContextOptionsBuilder<AMIdentityData>();
            identityOptionsBuilder.UseSqlServer(config.GetConnectionString("IdentityConnectionString"));
            DbContextOptions<AMIdentityData> identityOptions = identityOptionsBuilder.Options;

            var providers = new List<ProviderModel>();
            var salt = string.Empty;
            var password = string.Empty;
            var _coreData = new AMCoreData(coreOptions, config);
            var _identityData = new AMIdentityData(identityOptions, config);

            providers = _coreData.Providers
                .Where(x => x.AccessGranted == false && x.DeleteDate == null && x.EMailVerified == true /*&& x.EMail.Equals(config["AcceptedProvider"])*/)
                .OrderBy(x => x.CreateDate)
                .Take(5)
                .ToList();


            foreach (var provider in providers)
            {
                salt = IdentityTool.GenerateSaltString();
                password = IdentityTool.GenerateRandomPassword();
                var passwordModel = new PasswordModel(provider.ProviderId, true, IdentityTool.HashPassword(password, salt), salt);

                var message = $"Good news! You can now use the system!{Environment.NewLine}" +
                    $"Temporary password: {password}";
                var providerComm = new ProviderCommunicationModel(provider.ProviderId, message, DateTime.MinValue);

                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    provider.AccessGranted = true;
                    _coreData.Providers.Update(provider);
                    _coreData.ProviderCommunications.Add(providerComm);
                    _coreData.SaveChanges();

                    _identityData.Passwords.Add(passwordModel);
                    _identityData.SaveChanges();

                    scope.Complete();
                }
            }
        }
    }
}

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
        private AMCoreData _coreData = default!;
        private AMIdentityData _identityData = default!;
        private IConfigurationRoot _config = default!;
        private AMDevLogger _logger = new();

        static async Task Main(string[] args)
        {
            var program = new Program();
            program._logger.LogInfo("+");

            program.LoadConfiguration();
            program.InitializeDataContexts();

            await program.GrantAccessAndNotifyProvidersAsync();

            program._logger.LogInfo("-");
        }

        private void LoadConfiguration()
        {
            _config = new ConfigurationManager()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        private void InitializeDataContexts()
        {
            var coreOptions = new DbContextOptionsBuilder<AMCoreData>()
                .UseSqlServer(_config.GetConnectionString("CoreConnectionString"))
                .Options;

            var identityOptions = new DbContextOptionsBuilder<AMIdentityData>()
                .UseSqlServer(_config.GetConnectionString("IdentityConnectionString"))
                .Options;

            _coreData = new AMCoreData(coreOptions, _config);
            _identityData = new AMIdentityData(identityOptions, _config);
        }

        private async Task GrantAccessAndNotifyProvidersAsync()
        {
            var providers = _coreData.Providers
                .Where(p => !p.AccessGranted && p.DeleteDate == null && p.EMailVerified)
                .OrderBy(p => p.CreateDate)
                .Take(5)
                .ToList();

            foreach (var provider in providers)
            {
                var salt = IdentityTool.GenerateSaltString();
                var password = IdentityTool.GenerateRandomPassword();
                var hashedPassword = IdentityTool.HashPassword(password, salt);

                var passwordModel = new PasswordModel(provider.ProviderId, true, hashedPassword, salt);

                var message = $"Good news! You can now use the system!{Environment.NewLine}" +
                    $"Please complete your profile to be able to schedule appointments and notifications." +
                    $"Temporary password: {password}";
                var communication = new ProviderCommunicationModel(provider.ProviderId, message, DateTime.MinValue);

                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                provider.AccessGranted = true;
                _coreData.Providers.Update(provider);
                _coreData.ProviderCommunications.Add(communication);
                _coreData.SaveChanges();

                _identityData.Passwords.Add(passwordModel);
                _identityData.SaveChanges();

                scope.Complete();

                _logger.LogAudit($"Granted access to Provider Id: {provider.ProviderId} | Temp password sent.");
            }
        }
    }
}
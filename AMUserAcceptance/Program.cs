using AMData.Models.CoreModels;
using AMData.Models.IdentityModels;
using AMServices.DataServices;
using AMTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMUserAcceptance;

internal class Program
{
    private readonly AMDevLogger _logger = new();
    private IConfigurationRoot _config = default!;
    private AMCoreData _coreData = default!;
    private AMIdentityData _identityData = default!;

    private static async Task Main(string[] args)
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
            .AddJsonFile("appsettings.json", false, true)
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

        _coreData = new AMCoreData(coreOptions, _config, _logger);
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

            provider.AccessGranted = true;
            try
            {
                // First context
                provider.AccessGranted = true;
                _coreData.Providers.Update(provider);
                _coreData.ProviderCommunications.Add(communication);
                await _coreData.SaveChangesAsync();

                // Second context
                _identityData.Passwords.Add(passwordModel);
                await _identityData.SaveChangesAsync();

                _logger.LogAudit($"Granted access to Provider Id: {provider.ProviderId} | Temp password sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());

                // Optional: Manual compensation logic if needed
                // Example: Roll back _coreData changes or mark as failed
                throw; // Rethrow or handle based on your use case
            }
        }
    }
}
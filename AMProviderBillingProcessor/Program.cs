using AMData.Models.CoreModels;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMProviderBillingProcessor;

internal class Program
{
    private static IConfiguration _config;
    private static AMDevLogger _logger;

    /*
     * Run at 5am pacific time
     */
    private static async Task Main(string[] args)
    {
        _logger = new AMDevLogger();
        _logger.LogInfo("+");

        try
        {
            InitializeConfiguration();

            await RunPayments();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
        finally
        {
            _logger.LogInfo("-");
        }
    }

    private static void InitializeConfiguration()
    {
        _config = (IConfiguration)new ConfigurationManager()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true);
    }

    private static async Task RunPayments()
    {
        _logger.LogAudit($"Running Payments for {DateTime.UtcNow.Date}");

        var todaysPayments = new List<ProviderModel>();

        var options = new DbContextOptionsBuilder<AMCoreData>()
            .UseSqlServer(_config.GetConnectionString("CoreConnectionString"))
            .Options;

        await using var coreData = new AMCoreData(options, _config, _logger);

        await coreData.ExecuteWithRetryAsync(async () =>
        {
            todaysPayments = await coreData.Providers
                .Where(x => x.NextBillingDate.HasValue &&
                            x.NextBillingDate.Value.Date <= DateTime.UtcNow.Date)
                .ToListAsync();
        });

        if (todaysPayments.Count == 0)
        {
            _logger.LogAudit("No Payments found");
            return;
        }

        var success = false;
        var providerComm = new ProviderCommunicationModel();
        foreach (var payment in todaysPayments)
        {
            _logger.LogAudit("Simulate running payment...");
            if (success)
            {
                _logger.LogAudit($"Payment for provider id {payment.ProviderId} succeeded");

                providerComm = new ProviderCommunicationModel(
                    payment.ProviderId,
                    $"Your payment was successfully processed. " +
                    $"Your next billing date will be {payment.NextBillingDate?.ToLocalTime().Date:MM/dd/yyyy}",
                    DateTime.MinValue);

                await coreData.ExecuteWithRetryAsync(async () =>
                {
                    payment.NextBillingDate = payment.NextBillingDate.Value.AddMonths(1);

                    await coreData.ProviderCommunications.AddAsync(providerComm);
                    await coreData.SaveChangesAsync();
                });
            }
            else
            {
                _logger.LogAudit($"Payment for provider id {payment.ProviderId} failed");

                providerComm = new ProviderCommunicationModel(
                    payment.ProviderId,
                    $"Your payment was not processed successfully. " +
                    $"Please update your payment method to avoid service interruption.",
                    DateTime.MinValue);

                await coreData.ExecuteWithRetryAsync(async () =>
                {
                    await coreData.ProviderCommunications.AddAsync(providerComm);
                    await coreData.SaveChangesAsync();
                });
            }
        }
    }
}
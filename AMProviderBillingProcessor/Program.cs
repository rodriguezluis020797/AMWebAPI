using AMData.Models.CoreModels;
using AMServices.PaymentEngineServices;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace AMProviderBillingProcessor;

internal class Program
{
    private static IConfiguration _config;
    private static AMDevLogger _logger;
    private static IProviderBillingService _providerBillingService;

    /*
     * Run at 5am pacific time
     */
    private static async Task Main(string[] args)
    {
        _logger = new AMDevLogger();
        _logger.LogInfo("+");
        _logger.LogAudit("+");

        try
        {
            InitializeConfiguration();
            
            _providerBillingService = new StripeProviderBillingService(_logger,  _config);
            
            await RunPayments();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }
        finally
        {
            _logger.LogInfo("-");
            _logger.LogAudit("-");
        }
    }

    private static void InitializeConfiguration()
    {
        _config = (IConfiguration)new ConfigurationManager()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true);
        
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
    }

    private static async Task RunPayments()
    {
        _logger.LogAudit($"Running Payments for {DateTime.UtcNow.Date}");

        var providersToCharge = new List<ProviderModel>();

        var options = new DbContextOptionsBuilder<AMCoreData>()
            .UseSqlServer(_config.GetConnectionString("CoreConnectionString"))
            .Options;

        await using var coreData = new AMCoreData(options, _config, _logger);

        await coreData.ExecuteWithRetryAsync(async () =>
        {
            providersToCharge = await coreData.Providers
                .Where(x => x.NextBillingDate.HasValue &&
                            x.NextBillingDate.Value.Date <= DateTime.UtcNow.Date)
                .Include(x => x.Clients)
                .ToListAsync();
        });

        if (providersToCharge.Count == 0)
        {
            _logger.LogAudit("No accounts to run found");
            return;
        }
        
        _logger.LogAudit($"Accounts to run: {providersToCharge.Count}");
        
        
        var success = false;
        var providerComm = new ProviderCommunicationModel();
        var providerLogPayment = new ProviderLogPayment();
        
        foreach (var payment in providersToCharge)
        {
            try
            {
                _logger.LogAudit($"Processing Provider Id: {payment.ProviderId}");
                var totalSMSMessages = long.MinValue;

                var todayMidnight = payment.NextBillingDate.Value;
                var oneMonthAgoMidnight = todayMidnight.AddMonths(-1);

                await coreData.ExecuteWithRetryAsync(async () =>
                {
                    totalSMSMessages = await coreData.ClientCommunications
                        .Where(cc =>
                            coreData.Clients
                                .Where(c => c.ProviderId == payment.ProviderId)
                                .Select(c => c.ClientId)
                                .Contains(cc.ClientId) &&
                            cc.Sent == true &&
                            cc.CreateDate >= oneMonthAgoMidnight &&
                            cc.CreateDate < todayMidnight)
                        .LongCountAsync();
                });

                _logger.LogAudit($"Total SMS Messages: {totalSMSMessages}");

                var invoiceItems = new List<InvoiceItemCreateOptions>
                {
                    new()
                    {
                        Customer = payment.PayEngineId,
                        Currency = "usd",
                        Description = "AM Tech Base Services",
                        Pricing = new InvoiceItemPricingOptions
                        {
                            Price = "price_1RTrwgPmRnZS7JiXUMMF3sQZ"
                        },
                        Quantity = 1
                    },
                    new()
                    {
                        Customer = payment.PayEngineId,
                        Currency = "usd",
                        Description = "SMS Messages",
                        Pricing = new InvoiceItemPricingOptions
                        {
                            Price = "price_1RTqikPmRnZS7JiXG1CWliRa"
                        },
                        Quantity = totalSMSMessages
                    }
                };

                _logger.LogAudit($"Running payment for provider id {payment.ProviderId} - " +
                                 $"price id: {invoiceItems.FirstOrDefault().Pricing.Price} - " +
                                 $"quantity: {invoiceItems.FirstOrDefault().Quantity}");

                var capturedPayment = new Invoice();

                await coreData.ExecuteWithRetryAsync(async () =>
                {
                    capturedPayment =
                        await _providerBillingService.CapturePayment(payment.PayEngineId, invoiceItems);
                });

                if (capturedPayment.Status.Equals("paid"))
                {
                    _logger.LogAudit($"Payment succeeded... sending e-mail");

                    providerComm = new ProviderCommunicationModel(
                        payment.ProviderId,
                        $"Your payment was successfully processed. " +
                        $"Your next billing date will be {payment.NextBillingDate?.ToLocalTime().Date:MM/dd/yyyy}",
                        DateTime.MinValue);

                    providerLogPayment = new ProviderLogPayment(payment.ProviderId, true, null);

                    await coreData.ExecuteWithRetryAsync(async () =>
                    {
                        payment.NextBillingDate = payment.NextBillingDate.Value.AddMonths(1);

                        await coreData.ProviderLogPayments.AddAsync(providerLogPayment);
                        await coreData.ProviderCommunications.AddAsync(providerComm);
                        await coreData.SaveChangesAsync();
                    });
                }
                else
                {
                    _logger.LogAudit($"Payment failed... sending e-mail");

                    providerComm = new ProviderCommunicationModel(
                        payment.ProviderId,
                        $"Your payment was not processed successfully. " +
                        $"Please update your payment method to avoid service interruption.",
                        DateTime.MinValue);

                    providerLogPayment = new ProviderLogPayment(payment.ProviderId, false, "");

                    await coreData.ExecuteWithRetryAsync(async () =>
                    {

                        await coreData.ProviderLogPayments.AddAsync(providerLogPayment);
                        await coreData.ProviderCommunications.AddAsync(providerComm);
                        await coreData.SaveChangesAsync();
                    });
                }
            }
            catch (Exception ex)
            {
                providerLogPayment = new ProviderLogPayment(payment.ProviderId, false, ex.Message);
                await coreData.ExecuteWithRetryAsync(async () =>
                {
                    await coreData.ProviderLogPayments.AddAsync(providerLogPayment);
                    await coreData.SaveChangesAsync();
                });
                _logger.LogAudit($"Error Processing Provider");
                _logger.LogError($"{ex.ToString()}");
            }
            finally
            {
                _logger.LogAudit($"Done Processing Provider Id: {payment.ProviderId}");
            }
        }
    }
}
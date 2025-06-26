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
                            x.NextBillingDate.Value.Date <= DateTime.UtcNow.Date &&
                            x.SubscriptionEnded == false)
                .Include(x => x.Clients)
                .ToListAsync();
        });

        if (providersToCharge.Count == 0)
        {
            _logger.LogAudit("No accounts to run found");
            return;
        }
        
        _logger.LogAudit($"Accounts to run: {providersToCharge.Count}");
        
        var providerComm = new ProviderCommunicationModel();
        var providerLogPayment = new ProviderLogPayment();
        
        foreach (var provider in providersToCharge)
        {
            try
            {
                _logger.LogAudit($"Processing Provider Id: {provider.ProviderId}");
                
                var totalSMSMessages = long.MinValue;
                
                await coreData.ExecuteWithRetryAsync(async () =>
                {
                    totalSMSMessages = await (
                        from cc in coreData.ClientCommunications
                        join c in coreData.Clients on cc.ClientId equals c.ClientId
                        where c.ProviderId == provider.ProviderId
                              && cc.Sent == true
                              && cc.Paid == false
                        select cc
                    ).LongCountAsync();
                });
                
                _logger.LogAudit($"Total SMS Messages: {totalSMSMessages}");

                if (provider.SubscriptionToBeCancelled)
                {
                    await coreData.ExecuteWithRetryAsync(async () =>
                    {
                        await coreData.Providers
                            .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.SubscriptionEnded, true));
                    
                        await coreData.ClientCommunications
                            .Where(x => provider.NextBillingDate.Value.Date < x.SendAfter)
                            .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.DeleteDate, DateTime.UtcNow));

                        await coreData.SaveChangesAsync(); 
                    });

                    if (totalSMSMessages < 1)
                    {
                        _logger.LogAudit($"Subscription cancelled and no messages to charge for {provider.ProviderId}");
                        return;
                    }
                }

                var invoiceItems = new List<InvoiceItemCreateOptions>
                {
                    new()
                    {
                        Customer = provider.PayEngineId,
                        Currency = "usd",
                        Description = "SMS Messages",
                        Pricing = new InvoiceItemPricingOptions
                        {
                            Price = "price_1RTqikPmRnZS7JiXG1CWliRa"
                        },
                        Quantity = totalSMSMessages
                    }
                };

                if (!provider.SubscriptionToBeCancelled)
                {
                    invoiceItems.Add(
                        new InvoiceItemCreateOptions
                        {
                        Customer = provider.PayEngineId,
                        Currency = "usd",
                        Description = "AM Tech Base Services",
                        Pricing = new InvoiceItemPricingOptions
                        {
                            Price = "price_1RTrwgPmRnZS7JiXUMMF3sQZ"
                        },
                        Quantity = 1
                    });
                }

                invoiceItems = invoiceItems
                    .OrderBy(x => x.Description, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var capturedPayment = new Invoice();

                await coreData.ExecuteWithRetryAsync(async () =>
                {
                    capturedPayment =
                        await _providerBillingService.CapturePayment(provider.PayEngineId, invoiceItems);
                });

                if (capturedPayment.Status.Equals("paid"))
                {
                    _logger.LogAudit($"Payment succeeded... sending e-mail");

                    providerComm = new ProviderCommunicationModel(
                        provider.ProviderId,
                        $"Your payment was successfully processed. " +
                        $"Your next billing date will be {provider.NextBillingDate?.ToLocalTime().Date:MM/dd/yyyy}",
                        DateTime.MinValue);

                    providerLogPayment = new ProviderLogPayment(provider.ProviderId, capturedPayment.AmountPaid, totalSMSMessages, true, null);

                    await coreData.ExecuteWithRetryAsync(async () =>
                    {
                        provider.NextBillingDate = provider.NextBillingDate.Value.AddMonths(1);
                        
                        await coreData.ClientCommunications
                            .Where(cc =>
                                cc.Sent == true &&
                                cc.Paid == false &&
                                coreData.Clients
                                    .Where(c => c.ProviderId == provider.ProviderId)
                                    .Select(c => c.ClientId)
                                    .Contains(cc.ClientId))
                            .ExecuteUpdateAsync(upd => upd.SetProperty(x => x.Paid, x => true));
                        
                        await coreData.ProviderLogPayments.AddAsync(providerLogPayment);
                        await coreData.ProviderCommunications.AddAsync(providerComm);
                        await coreData.SaveChangesAsync();
                    });
                }
                else
                {
                    _logger.LogAudit($"Payment failed... sending e-mail");

                    providerComm = new ProviderCommunicationModel(
                        provider.ProviderId,
                        $"Your payment was not processed successfully. " +
                        $"Please update your payment method to avoid service interruption.",
                        DateTime.MinValue);

                    providerLogPayment = new ProviderLogPayment(provider.ProviderId, 0, 0, false, "Unable to process payment");

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
                providerLogPayment = new ProviderLogPayment(provider.ProviderId, 0, 0, false, ex.Message);
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
                _logger.LogAudit($"Done Processing Provider Id: {provider.ProviderId}");
            }
        }
    }
    
}
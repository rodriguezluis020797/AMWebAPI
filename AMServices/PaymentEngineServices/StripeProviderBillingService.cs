using AMData.Models.CoreModels;
using AMTools.Tools;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace AMServices.PaymentEngineServices;

public interface IProviderBillingService
{
    Task<string> CreateProviderBillingProfileAsync(string eMail, string businessName, string firstName,
        string? middleName, string lastName);

    Task UpdateProviderBillingProfile(ProviderModel provider);

    Task<Invoice> CapturePayment(string customerPayEngineId, List<InvoiceItemCreateOptions> invoiceItems);
}

public class StripeProviderBillingService (IAMLogger logger, IConfiguration config) : IProviderBillingService
{
    public async Task<string> CreateProviderBillingProfileAsync(string eMail, string businessName, string firstName,
        string? middleName, string lastName)
    {
        var customerService = new CustomerService();
        var customerCreateOptions = new CustomerCreateOptions
        {
            Email = eMail,
            Name = string.IsNullOrEmpty(middleName)
                ? $"{businessName} - {firstName} {lastName}"
                : $"{businessName} - {firstName} {middleName} {lastName}"
        };

        var customerCreateResult = await customerService.CreateAsync(customerCreateOptions);

        return customerCreateResult.Id;
    }

    public async Task UpdateProviderBillingProfile(ProviderModel provider)
    {
        var customerService = new CustomerService();
        var options = new CustomerUpdateOptions
        {
            Name = $"{provider.BusinessName} - {provider.FirstName} {provider.LastName}",
            Email = provider.EMail,
            Address = new AddressOptions
            {
                Line1 = provider.AddressLine1,
                Line2 = provider.AddressLine2,
                City = provider.City,
                PostalCode = provider.ZipCode,
                Country = provider.CountryCode.ToString().Replace('_', ' '),
                State = provider.StateCode.ToString().Split('_')[1]
            }
        };
        
        await customerService
            .UpdateAsync(provider.PayEngineId, options);
    }

    public async Task<Invoice> CapturePayment(string customerPayEngineId, List<InvoiceItemCreateOptions> invoiceItems)
    {
        var invoiceService = new InvoiceService();
        var invoiceItemService = new InvoiceItemService();
        
        var invoice = await invoiceService.CreateAsync(new InvoiceCreateOptions
        {
            Customer = customerPayEngineId,
            AutoAdvance = false,
            CollectionMethod = "charge_automatically",
            PaymentSettings = new InvoicePaymentSettingsOptions
            {
                PaymentMethodTypes = new List<string> { "card" }
            }
        });
        
        foreach (var item in invoiceItems)
        {
            item.Invoice = invoice.Id;
            await invoiceItemService.CreateAsync(item);
        }

        //await Task.Delay(1000);
        var finalizedInvoice = await invoiceService.FinalizeInvoiceAsync(invoice.Id);

        //await Task.Delay(2000);
        return await invoiceService.PayAsync(invoice.Id);
    }
}
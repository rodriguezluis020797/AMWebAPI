using AMData.Models.CoreModels;
using AMTools;
using Stripe;

namespace AMServices.PaymentEngineServices;

public interface IProviderBillingService
{
    Task<string> CreateProviderBillingProfileAsync(string eMail, string businessName, string firstName,
        string? middleName, string lastName);

    Task UpdateProviderBillingProfile(ProviderModel provider);

    Task<Invoice> CapturePayment(string customerPayEngineId, List<InvoiceItemCreateOptions> invoiceItems);
    Task<bool> IsThereADefaultPaymentMetho(string paymentEngineId);
}

public class StripeProviderBillingService(IAMLogger logger) : IProviderBillingService
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
        logger.LogAudit($"attempting to capture payment for pay engine id {customerPayEngineId}");
        var invoiceService = new InvoiceService();
        var invoiceItemService = new InvoiceItemService();

        var invoice = await invoiceService.CreateAsync(new InvoiceCreateOptions
        {
            Customer = customerPayEngineId,
            AutoAdvance = false,
            CollectionMethod = "charge_automatically",
            PaymentSettings = new InvoicePaymentSettingsOptions
            {
                PaymentMethodTypes = ["card"]
            }
        });

        foreach (var item in invoiceItems)
        {
            item.Invoice = invoice.Id;
            await invoiceItemService.CreateAsync(item);
        }

        await invoiceService.FinalizeInvoiceAsync(invoice.Id);

        return await invoiceService.PayAsync(invoice.Id);
    }

    public async Task<bool> IsThereADefaultPaymentMetho(string payEngineId)
    {
        var customerService = new CustomerService();
        var customer = await customerService.GetAsync(payEngineId);

        return !string.IsNullOrEmpty(customer.InvoiceSettings.DefaultPaymentMethodId);
    }
}
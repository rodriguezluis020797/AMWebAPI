using Stripe;
using Stripe.Checkout;

namespace AMServices.PaymentEngineServices;

public interface IProviderBillingService
{
    public Task<string> CreateProviderBillingProfileAsync(string eMail, string businessName, string firstName,
        string? middleName,
        string lastName);

    public Task<string> CreateProviderSession(string providerPayEngineId, string sessionMode);
    public bool CreateBill(string billingServiceproviderId);
    public bool IssueRefund(string billingServiceproviderId);
}

public class StripeProviderBillingService : IProviderBillingService
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

    public async Task<string> CreateProviderSession(string providerPayEngineId, string sessionMode)
    {
        var options = new SessionCreateOptions
        {
            Mode = "setup",
            Customer = providerPayEngineId, // The Stripe Customer ID
            PaymentMethodTypes = new List<string> { "card" },
            SuccessUrl = "https://yourapp.com/setup-success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = "https://yourapp.com/setup-cancelled"
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return session.Id;
    }

    public bool CreateBill(string billingServiceproviderId)
    {
        throw new NotImplementedException();
    }

    public bool IssueRefund(string billingServiceproviderId)
    {
        throw new NotImplementedException();
    }
}
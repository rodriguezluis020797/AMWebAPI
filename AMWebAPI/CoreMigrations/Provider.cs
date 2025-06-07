namespace AMWebAPI.CoreMigrations;

public class Provider
{
    public long ProviderId { get; set; }

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public bool EmailVerified { get; set; }

    public int CountryCode { get; set; }

    public int StateCode { get; set; }

    public int TimeZoneCode { get; set; }

    public bool AccessGranted { get; set; }

    public DateTime? LastLogindate { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? UpdateDate { get; set; }

    public DateTime? DeleteDate { get; set; }

    public string BusinessName { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime TrialEndDate { get; set; }

    public string? PayEngineId { get; set; }

    public string AddressLine1 { get; set; } = null!;

    public string? AddressLine2 { get; set; }

    public string City { get; set; } = null!;

    public string ZipCode { get; set; } = null!;

    public DateTime? NextBillingDate { get; set; }

    public bool SubscriptionToBeCancelled { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();

    public virtual ICollection<ProviderAlert> ProviderAlerts { get; set; } = new List<ProviderAlert>();

    public virtual ICollection<ProviderBilling> ProviderBillings { get; set; } = new List<ProviderBilling>();

    public virtual ICollection<ProviderCommunication> ProviderCommunications { get; set; } =
        new List<ProviderCommunication>();

    public virtual ICollection<ProviderReview> ProviderReviews { get; set; } = new List<ProviderReview>();

    public virtual ICollection<ResetPasswordRequest> ResetPasswordRequests { get; set; } =
        new List<ResetPasswordRequest>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<UpdateProviderEmailRequest> UpdateProviderEmailRequests { get; set; } =
        new List<UpdateProviderEmailRequest>();

    public virtual VerifyProviderEmailRequest? VerifyProviderEmailRequest { get; set; }
}
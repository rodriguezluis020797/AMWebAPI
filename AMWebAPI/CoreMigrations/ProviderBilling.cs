namespace AMWebAPI.CoreMigrations;

public class ProviderBilling
{
    public long ProviderBillingId { get; set; }

    public long ProviderId { get; set; }

    public long Amount { get; set; }

    public long DiscountAmount { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? PaidDate { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? UpdateDate { get; set; }

    public DateTime? DeleteDate { get; set; }

    public virtual Provider Provider { get; set; } = null!;
}
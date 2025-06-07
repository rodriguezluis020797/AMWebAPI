namespace AMWebAPI.CoreMigrations;

public class UpdateProviderEmailRequest
{
    public long UpdateProviderEmailRequestId { get; set; }

    public long ProviderId { get; set; }

    public string QueryGuid { get; set; } = null!;

    public string NewEmail { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public DateTime? DeleteDate { get; set; }

    public virtual Provider Provider { get; set; } = null!;
}
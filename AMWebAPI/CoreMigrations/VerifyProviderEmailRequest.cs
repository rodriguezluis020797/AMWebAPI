namespace AMWebAPI.CoreMigrations;

public class VerifyProviderEmailRequest
{
    public long VerifyProviderEmailRequestId { get; set; }

    public long ProviderId { get; set; }

    public string QueryGuid { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public DateTime? DeleteDate { get; set; }

    public virtual Provider Provider { get; set; } = null!;
}
namespace AMWebAPI.CoreMigrations;

public class ProviderAlert
{
    public long ProviderAlertId { get; set; }

    public long ProviderId { get; set; }

    public string Alert { get; set; } = null!;

    public DateTime AlertAfterDate { get; set; }

    public bool Acknowledged { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? AcknowledgedDate { get; set; }

    public DateTime? DeleteDate { get; set; }

    public virtual Provider Provider { get; set; } = null!;
}
namespace AMWebAPI.CoreMigrations;

public class Session
{
    public long SessionId { get; set; }

    public long ProviderId { get; set; }

    public DateTime CreateDate { get; set; }

    public virtual Provider Provider { get; set; } = null!;

    public virtual ICollection<SessionAction> SessionActions { get; set; } = new List<SessionAction>();
}
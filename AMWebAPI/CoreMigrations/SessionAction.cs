namespace AMWebAPI.CoreMigrations;

public class SessionAction
{
    public long SessionActionId { get; set; }

    public long SessionId { get; set; }

    public int SessionAction1 { get; set; }

    public DateTime CreateDate { get; set; }

    public virtual Session Session { get; set; } = null!;
}
namespace AMWebAPI.CoreMigrations;

public class ClientNote
{
    public long ClientNoteId { get; set; }

    public long ClientId { get; set; }

    public string Note { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public DateTime? UpdateDate { get; set; }

    public DateTime? DeleteDate { get; set; }

    public virtual Client Client { get; set; } = null!;
}
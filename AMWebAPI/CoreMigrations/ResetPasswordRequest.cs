namespace AMWebAPI.CoreMigrations;

public class ResetPasswordRequest
{
    public long ResetPasswordId { get; set; }

    public long ProviderId { get; set; }

    public string QueryGuid { get; set; } = null!;

    public bool Reset { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? UpdateDate { get; set; }

    public DateTime? DeleteDate { get; set; }

    public virtual Provider Provider { get; set; } = null!;
}
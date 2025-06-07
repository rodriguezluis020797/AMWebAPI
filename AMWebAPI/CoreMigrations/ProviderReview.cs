namespace AMWebAPI.CoreMigrations;

public class ProviderReview
{
    public long ProviderReviewId { get; set; }

    public string GuidQuery { get; set; } = null!;

    public long ProviderId { get; set; }

    public string? ReviewText { get; set; }

    public decimal? Rating { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? DeleteDate { get; set; }

    public long ClientId { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual Provider Provider { get; set; } = null!;
}
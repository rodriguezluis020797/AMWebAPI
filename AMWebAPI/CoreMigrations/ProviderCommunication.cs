namespace AMWebAPI.CoreMigrations;

public class ProviderCommunication
{
    public long ProviderCommunicationId { get; set; }

    public long ProviderId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime SendAfter { get; set; }

    public bool Sent { get; set; }

    public DateTime? AttemptOne { get; set; }

    public DateTime? AttemptTwo { get; set; }

    public DateTime? AttemptThree { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? DeleteDate { get; set; }

    public virtual Provider Provider { get; set; } = null!;
}
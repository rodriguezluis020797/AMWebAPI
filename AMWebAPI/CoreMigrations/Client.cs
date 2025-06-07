namespace AMWebAPI.CoreMigrations;

public class Client
{
    public long ClientId { get; set; }

    public long ProviderId { get; set; }

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public DateTime? UpdateDate { get; set; }

    public DateTime? DeleteDate { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<ClientCommunication> ClientCommunications { get; set; } =
        new List<ClientCommunication>();

    public virtual ICollection<ClientNote> ClientNotes { get; set; } = new List<ClientNote>();

    public virtual Provider Provider { get; set; } = null!;

    public virtual ICollection<ProviderReview> ProviderReviews { get; set; } = new List<ProviderReview>();
}
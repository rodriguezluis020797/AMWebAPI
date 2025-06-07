namespace AMWebAPI.CoreMigrations;

public class Service
{
    public long ServiceId { get; set; }

    public long ProviderId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool AllowClientScheduling { get; set; }

    public decimal Price { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? DeleteDate { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual Provider Provider { get; set; } = null!;
}
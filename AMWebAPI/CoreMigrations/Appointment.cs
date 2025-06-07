namespace AMWebAPI.CoreMigrations;

public class Appointment
{
    public long AppointmentId { get; set; }

    public long ServiceId { get; set; }

    public long ClientId { get; set; }

    public long ProviderId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? Notes { get; set; }

    public int Status { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime? UpdateDate { get; set; }

    public DateTime? DeleteDate { get; set; }

    public decimal Price { get; set; }

    public bool OverridePrice { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual Provider Provider { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
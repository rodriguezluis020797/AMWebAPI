using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels;

[Table("Appointment")]
public class AppointmentModel
{
    public AppointmentModel(long serviceId, long clientId, long providerId, DateTime startDate, DateTime endDate, string notes)
    {
        ServiceId = serviceId;
        ClientId = clientId;
        ProviderId = providerId;
        StartDate = startDate;
        EndDate = endDate;
        Notes = notes;
    }

    [Key] public long AppointmentId { get; set; }
    [ForeignKey("Service")] public long ServiceId { get; set; }
    [ForeignKey("Client")] public long ClientId { get; set; }
    [ForeignKey("Provider")] public long ProviderId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
    public AppointmentStatusEnum Status { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public DateTime? DeleteDate { get; set; }
    [NotMapped] public ClientModel Client { get; set; }
    [NotMapped] public ProviderModel Provider { get; set; }
}
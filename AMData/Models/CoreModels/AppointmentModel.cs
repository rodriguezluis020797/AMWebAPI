using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AMData.Models.DTOModels;

namespace AMData.Models.CoreModels;

[Table("Appointment")]
public class AppointmentModel
{
    public AppointmentModel(long serviceId, long clientId, long providerId, DateTime startDate, DateTime endDate,
        string notes)
    {
        ServiceId = serviceId;
        ClientId = clientId;
        ProviderId = providerId;
        StartDate = startDate;
        EndDate = endDate;
        Notes = string.IsNullOrEmpty(notes) ? null : notes;
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
    [NotMapped] public virtual ClientModel Client { get; set; }
    [NotMapped] public virtual ProviderModel Provider { get; set; }
    [NotMapped] public virtual ServiceModel Service { get; set; }

    public void UpdateRecrodFromDTO(AppointmentDTO dto)
    {
        StartDate = dto.StartDate;
        EndDate = dto.EndDate;
        Notes = string.IsNullOrEmpty(dto.Notes) ? null : dto.Notes;
        Status = dto.Status;
        UpdateDate = DateTime.UtcNow;
    }
}
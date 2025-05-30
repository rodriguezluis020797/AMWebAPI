using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AMData.Models.DTOModels;

namespace AMData.Models.CoreModels;

[Table("Appointment")]
public class AppointmentModel
{
    public AppointmentModel(){}
    public AppointmentModel(long serviceId, long clientId, long providerId, AppointmentStatusEnum status, DateTime startDate, DateTime endDate,
        string notes, decimal price)
    {
        AppointmentId = 0;
        ServiceId = serviceId;
        ClientId = clientId;
        ProviderId = providerId;
        StartDate = startDate;
        EndDate = endDate;
        Notes = string.IsNullOrEmpty(notes) ? null : notes;
        Price = price;
        Status = status;
        CreateDate = DateTime.UtcNow;
        UpdateDate = null;
        DeleteDate = null;
    }
    [Key] public long AppointmentId { get; set; } = 0;
    [ForeignKey("Service")] public long ServiceId { get; set; }
    [ForeignKey("Client")] public long ClientId { get; set; }
    [ForeignKey("Provider")] public long ProviderId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Notes { get; set; }
    public decimal Price { get; set; }
    public AppointmentStatusEnum Status { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public DateTime? DeleteDate { get; set; }
    [NotMapped] public virtual ClientModel Client { get; set; }
    [NotMapped] public virtual ProviderModel Provider { get; set; }
    [NotMapped] public virtual ServiceModel Service { get; set; }
    
    public void UpdateRecrodFromDto(AppointmentDTO dto)
    {
        StartDate = dto.StartDate;
        EndDate = dto.EndDate;
        Notes = string.IsNullOrEmpty(dto.Notes) ? null : dto.Notes;
        Status = dto.Status;
        UpdateDate = DateTime.UtcNow;
        Price = dto.Price;
    }
}
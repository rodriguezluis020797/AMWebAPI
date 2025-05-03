namespace AMData.Models.DTOModels;

public class AppointmentDTO
{
    public string ServiceId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = new DateTime();
    public DateTime EndDate { get; set; } = new DateTime();
    public string Notes { get; set; } = string.Empty;
    public AppointmentStatusEnum Status { get; set; } = AppointmentStatusEnum.Unknown;
}
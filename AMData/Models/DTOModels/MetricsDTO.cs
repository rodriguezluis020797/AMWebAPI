using AMData.Models.CoreModels;

namespace AMData.Models.DTOModels;

public class MetricsDTO : BaseDTO
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<AppointmentDTO> Appointments { get; set; } = new();
    public Dictionary<string, string> ServiceNames { get; set; } = new();
    public decimal TotalEarnings { get; set; }
    public decimal TotalScheduledProjectedEarnings { get; set; }
    public decimal TotalCompletedEarnings { get; set; }

    public void Validate()
    {
        if (EndDate < StartDate) ErrorMessage = "End date must be after start date.";
    }

    public void CreateNewRecordFromModel(AppointmentModel appointment)
    {
        var dto = new AppointmentDTO();
        dto.CreateNewRecordFromModel(appointment);

        Appointments.Add(dto);
        if (!ServiceNames.ContainsKey(appointment.Service.ServiceId.ToString()))
            ServiceNames.Add(appointment.Service.ServiceId.ToString(), appointment.Service.Name);
    }

    public void CalculateMetrics()
    {
        TotalCompletedEarnings = 0;
        TotalScheduledProjectedEarnings = 0;
        TotalEarnings = 0;
        foreach (var appointment in Appointments)
        {
            switch (appointment.Status)
            {
                case AppointmentStatusEnum.Completed:
                    TotalCompletedEarnings += appointment.Price;
                    break;
                case AppointmentStatusEnum.Scheduled:
                    TotalScheduledProjectedEarnings += appointment.Price;
                    break;
                case AppointmentStatusEnum.Select:
                case AppointmentStatusEnum.Cancelled:
                default:
                    throw new Exception(nameof(appointment.Status));
            }

            TotalEarnings += appointment.Price;
        }
    }
}
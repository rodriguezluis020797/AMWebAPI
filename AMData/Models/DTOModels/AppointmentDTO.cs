using AMData.Models.CoreModels;

namespace AMData.Models.DTOModels;

public class AppointmentDTO : BaseDTO
{
    public string AppointmentId { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public AppointmentStatusEnum Status { get; set; } = AppointmentStatusEnum.Unknown;

    public void Validate()
    {
        //Make sure time is already converted to utc.
        if (StartDate < DateTime.UtcNow)
        {
            ErrorMessage = "Start date must be in the future.";
            return;
        }

        if (EndDate < StartDate)
        {
            ErrorMessage = "End date must be after start date.";
            return;
        }

        Notes = Notes.Trim();
    }

    public void CreateNewRecordFromModel(AppointmentModel model)
    {
        AppointmentId = model.AppointmentId.ToString();
        ServiceId = model.ServiceId.ToString();
        ClientId = model.ClientId.ToString();
        StartDate = model.StartDate;
        EndDate = model.EndDate;
        Notes = model.Notes ?? string.Empty;
        Status = AppointmentStatusEnum.Scheduled;
        ServiceName = model.Service.Name;
        ClientName = string.IsNullOrEmpty(model.Client.MiddleName)
            ? $"{model.Client.FirstName} {model.Client.LastName}"
            : $"{model.Client.FirstName} {model.Client.MiddleName} {model.Client.LastName}";
    }
}
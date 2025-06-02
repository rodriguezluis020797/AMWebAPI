using AMData.Models.CoreModels;
using AMTools;
using AMTools.Tools;

namespace AMData.Models.DTOModels;

public class AppointmentDTO : BaseDTO
{
    public string AppointmentId { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public bool OverridePrice { get; set; } = false;
    public decimal Price { get; set; } = decimal.Zero;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public AppointmentStatusEnum Status { get; set; } = AppointmentStatusEnum.Unknown;

    public void Validate()
    {
        ValidationTool.ValidateName(ClientId, out var cIdOutput);
            ClientId = cIdOutput;
            ErrorMessage = string.IsNullOrEmpty(ClientId) ? "Please select client." : string.Empty;
            if (!string.IsNullOrEmpty(ErrorMessage)) return;

            ValidationTool.ValidateName(ServiceId, out var sIdOutput);
            ServiceId = sIdOutput;
            ErrorMessage = string.IsNullOrEmpty(ServiceId) ? "Please select service." : string.Empty;
            if (!string.IsNullOrEmpty(ErrorMessage)) return;

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
        Status = model.Status;
        ServiceName = model.Service.Name;
        Price = model.Price;
        ClientName = string.IsNullOrEmpty(model.Client.MiddleName)
            ? $"{model.Client.FirstName} {model.Client.LastName}"
            : $"{model.Client.FirstName} {model.Client.MiddleName} {model.Client.LastName}";
    }
}
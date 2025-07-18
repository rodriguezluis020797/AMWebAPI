using AMData.Models.CoreModels;
using MCCDotnetTools;

namespace AMData.Models.DTOModels;

public class ClientDTO : BaseDTO
{
    public string ClientId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public void Validate()
    {
        // Validate First Name
        MCCValidationTool.ValidateName(FirstName, out var fnOutput);
        FirstName = fnOutput;
        ErrorMessage = string.IsNullOrEmpty(FirstName) ? "Please enter first name." : string.Empty;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;

        // Validate Middle Name
        MCCValidationTool.ValidateName(MiddleName, out var mnOutput);
        MiddleName = mnOutput;
        MiddleName = string.IsNullOrEmpty(MiddleName) ? null : MiddleName;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;

        // Validate Last Name
        MCCValidationTool.ValidateName(LastName, out var lnOutput);
        LastName = lnOutput;
        ErrorMessage = string.IsNullOrEmpty(LastName) ? "Please enter last name." : string.Empty;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;

        MCCValidationTool.IsValidPhoneNumber(PhoneNumber, out var pnOutput);
        PhoneNumber = pnOutput;
        ErrorMessage = string.IsNullOrEmpty(PhoneNumber) ? "Please enter valid 10 digit phone number." : string.Empty;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;
    }

    public void CreateRecordFromModel(ClientModel model)
    {
        ClientId = model.ClientId.ToString();
        FirstName = model.FirstName;
        MiddleName = model.MiddleName;
        LastName = model.LastName;
        PhoneNumber = model.PhoneNumber;
    }
}
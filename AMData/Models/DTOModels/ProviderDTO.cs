using AMData.Models.CoreModels;
using MCCDotnetTools;

namespace AMData.Models.DTOModels;

public class ProviderDTO : BaseDTO
{
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string EMail { get; set; } = string.Empty;
    public CountryCodeEnum CountryCode { get; set; } = CountryCodeEnum.Select;
    public StateCodeEnum StateCode { get; set; } = StateCodeEnum.Select;
    public TimeZoneCodeEnum TimeZoneCode { get; set; } = TimeZoneCodeEnum.Select;
    public bool HasLoggedIn { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public bool IsTempPassword { get; set; }
    public string PayEngineInfoUrl { get; set; } = string.Empty;
    public DateTime? NextBillingDate { get; set; }
    public AccountStatusEnum AccountStatus { get; set; }

    public void CreateNewRecordFromModel(ProviderModel provider, string payEngineInfoUrl)
    {
        ErrorMessage = null;
        IsSpecialCase = null;
        FirstName = provider.FirstName;
        MiddleName = provider.MiddleName;
        LastName = provider.LastName;
        EMail = provider.EMail;
        CountryCode = provider.CountryCode;
        StateCode = provider.StateCode;
        TimeZoneCode = provider.TimeZoneCode;
        HasLoggedIn = provider.LastLogindate != null;
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        IsTempPassword = false;
        BusinessName = provider.BusinessName;
        AddressLine1 = provider.AddressLine1;
        AddressLine2 = provider.AddressLine2;
        City = provider.City;
        ZipCode = provider.ZipCode;
        PayEngineInfoUrl = payEngineInfoUrl;
        NextBillingDate = provider.NextBillingDate;
        provider.AccountStatus = provider.AccountStatus;
        Description = provider.Description;
        AccountStatus = provider.AccountStatus;
    }

    public void Validate()
    {
        // Validate Business Name
        MCCValidationTool.ValidateName(BusinessName, out var bnOutput);
        BusinessName = bnOutput;
        ErrorMessage = string.IsNullOrEmpty(BusinessName) ? "Please enter business name." : string.Empty;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;

        // Validate Business Description
        MCCValidationTool.ValidateName(Description, out var bdOutput);
        Description = bdOutput;
        ErrorMessage = string.IsNullOrEmpty(Description) ? "Please enter description." : string.Empty;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;

        // Validate First Name
        MCCValidationTool.ValidateName(FirstName, out var fnOutput);
        FirstName = fnOutput;
        ErrorMessage = string.IsNullOrEmpty(FirstName) ? "Please enter first name." : string.Empty;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;

        // Validate Middle Name
        MCCValidationTool.ValidateName(MiddleName, out var mnOutput);
        MiddleName = mnOutput;
        MiddleName = string.IsNullOrEmpty(MiddleName) ? null : MiddleName;

        // Validate Last Name
        MCCValidationTool.ValidateName(LastName, out var lnOutput);
        LastName = lnOutput;
        ErrorMessage = string.IsNullOrEmpty(LastName) ? "Please enter last name." : string.Empty;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;

        // Validate Address Line 1
        MCCValidationTool.ValidateName(AddressLine1, out var al1Output);
        AddressLine1 = al1Output;
        ErrorMessage = string.IsNullOrEmpty(AddressLine1) ? "Please enter address line 1." : string.Empty;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;

        // Validate Address Line 2
        MCCValidationTool.ValidateName(AddressLine2, out var al2Output);
        AddressLine2 = al2Output;
        AddressLine2 = string.IsNullOrEmpty(AddressLine2) ? null : AddressLine2;

        MCCValidationTool.ValidateZipCode(ZipCode, out var zcOutput);
        ZipCode = zcOutput;
        ErrorMessage = string.IsNullOrEmpty(ZipCode) ? "Please enter valid 5 digit zip code." : string.Empty;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;

        // Validate Email
        if (!string.IsNullOrEmpty(EMail))
        {
            EMail = EMail.Trim();
            ErrorMessage = !MCCValidationTool.IsValidEmail(EMail) ? "Please enter valid e-mail." : string.Empty;
        }

        if (!string.IsNullOrEmpty(ErrorMessage)) return;

        ErrorMessage = CountryCode == CountryCodeEnum.Select ? "Please select country." : string.Empty;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;

        ErrorMessage = StateCode == StateCodeEnum.Select ? "Please select state." : string.Empty;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;

        ErrorMessage = TimeZoneCode == TimeZoneCodeEnum.Select ? "Please select time zone." : string.Empty;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;
    }
}
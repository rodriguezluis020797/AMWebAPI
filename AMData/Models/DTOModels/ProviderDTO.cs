using AMData.Models.CoreModels;
using AMTools.Tools;

namespace AMData.Models.DTOModels
{
    public class ProviderDTO : BaseDTO
    {
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string EMail { get; set; } = string.Empty;
        public CountryCodeEnum CountryCode { get; set; } = CountryCodeEnum.Select;
        public StateCodeEnum StateCode { get; set; } = StateCodeEnum.Select;
        public TimeZoneCodeEnum TimeZoneCode { get; set; } = TimeZoneCodeEnum.Select;
        public bool HasCompletedSignUp { get; set; } = false;
        public bool HasLoggedIn { get; set; } = false;
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public bool IsTempPassword { get; set; } = false;

        public void CreateNewRecordFromModel(ProviderModel provider)
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
            HasLoggedIn = provider.LastLogindate != null ? true : false;
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            IsTempPassword = false;
            HasCompletedSignUp = CountryCode != CountryCodeEnum.Select && StateCode != StateCodeEnum.Select && TimeZoneCode != TimeZoneCodeEnum.Select;
        }
        public void Validate()
        {
            // Validate First Name
            ValidationTool.ValidateName(FirstName, out string fnOutput);
            FirstName = fnOutput;
            ErrorMessage = string.IsNullOrEmpty(FirstName) ? "Please enter first name." : string.Empty;
            if (!string.IsNullOrEmpty(ErrorMessage)) return;

            // Validate Middle Name
            ValidationTool.ValidateName(MiddleName, out string mnOutput);
            MiddleName = mnOutput;
            MiddleName = string.IsNullOrEmpty(MiddleName) ? null : MiddleName;
            if (!string.IsNullOrEmpty(ErrorMessage)) return;

            // Validate Last Name
            ValidationTool.ValidateName(LastName, out string lnOutput);
            LastName = lnOutput;
            ErrorMessage = string.IsNullOrEmpty(LastName) ? "Please enter last name." : string.Empty;
            if (!string.IsNullOrEmpty(ErrorMessage)) return;

            // Validate Email
            if (!string.IsNullOrEmpty(EMail))
            {
                EMail = EMail.Trim();
                ErrorMessage = !ValidationTool.IsValidEmail(EMail) ? "Please enter valid e-mail." : string.Empty;
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
}

using AMData.Models.CoreModels;
using AMData.Models.DTOModels;
using AMTools.Tools;

namespace AMWebAPI.Models.DTOModels
{
    public class ProvidderDTO : BaseDTO
    {
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string EMail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsTempPassword { get; set; } = false;

        public void CreateNewRecordFromModel(ProviderModel user)
        {
            base.ResetModel();
            FirstName = user.FirstName;
            MiddleName = user.MiddleName;
            LastName = user.LastName;
            EMail = user.EMail;
            Password = string.Empty;
            IsTempPassword = false;
        }
        public void Validate()
        {
            ValidationTool.ValidateName(FirstName, out string fnOutput);
            FirstName = fnOutput;
            if (string.IsNullOrEmpty(FirstName))
            {
                ErrorMessage = "Please enter first name.";
            }

            ValidationTool.ValidateName(MiddleName, out string mnOutput);
            MiddleName = mnOutput;
            if (string.IsNullOrEmpty(MiddleName))
            {
                MiddleName = null;
            }

            ValidationTool.ValidateName(LastName, out string lnOutput);
            LastName = lnOutput;
            if (string.IsNullOrEmpty(LastName))
            {
                ErrorMessage = "Please enter last name.";
            }

            if (!string.IsNullOrEmpty(EMail))
            {
                EMail = EMail.Trim();

                if (!ValidationTool.IsValidEmail(EMail))
                {
                    ErrorMessage = "Please enter valid e-mail.";
                }
            }
        }
    }
}

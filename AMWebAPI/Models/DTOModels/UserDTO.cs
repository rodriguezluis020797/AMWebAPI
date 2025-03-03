using AMWebAPI.Models.CoreModels;
using AMWebAPI.Tools;

namespace AMWebAPI.Models.DTOModels
{
    public class UserDTO : BaseDTO
    {
        public string UserId { get; set; } = default;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string EMail { get; set; } = string.Empty;

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

        public void CreateNewRecordFromModel(UserModel user)
        {
            UserId = Uri.EscapeDataString(EncryptionTool.Encrypt(user.UserId.ToString()));
            FirstName = user.FirstName;
            MiddleName = user.MiddleName;
            LastName = user.LastName;
            EMail = user.EMail;
        }
    }
}

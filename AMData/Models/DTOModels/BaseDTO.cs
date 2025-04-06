namespace AMData.Models.DTOModels
{
    public class BaseDTO
    {
        public string ErrorMessage { get; set; } = string.Empty;

        public void ResetModel()
        {
            ErrorMessage = string.Empty;
        }
    }
}

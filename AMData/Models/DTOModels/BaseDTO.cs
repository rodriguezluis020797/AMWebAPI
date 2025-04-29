namespace AMData.Models.DTOModels
{
    public class BaseDTO
    {
        public string? ErrorMessage { get; set; } = null;
        public bool? IsSpecialCase { get; set; } = null;

        public void ResetModel()
        {
            ErrorMessage = null;
            IsSpecialCase = null;
        }
    }
}

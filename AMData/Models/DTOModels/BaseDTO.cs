namespace AMData.Models.DTOModels;

public class BaseDTO
{
    public string? ErrorMessage { get; set; }
    public bool? IsSpecialCase { get; set; }

    public void ResetModel()
    {
        ErrorMessage = null;
        IsSpecialCase = null;
    }
}
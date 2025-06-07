using AMData.Models.CoreModels;
using AMTools.Tools;

namespace AMData.Models.DTOModels;

public class ProviderReviewDTO : BaseDTO
{
    public string ProviderReviewId { get; set; }
    public string ProviderName { get; set; }
    public string GuidQuery { get; set; }
    public string ReviewText { get; set; }
    public decimal Rating { get; set; }
    public DateTime CreateDate { get; set; }

    public void Validate()
    {
        // Validate First Name
        ValidationTool.ValidateName(ReviewText, out var rtOutput);
        ReviewText = rtOutput;
        ErrorMessage = string.IsNullOrEmpty(ReviewText) ? "Please enter valid review." : string.Empty;
        if (!string.IsNullOrEmpty(ErrorMessage)) return;

        if (Rating is < 0 or > 5) ErrorMessage = "Please enter valid rating between 0 and 5.";
        if (!string.IsNullOrEmpty(ErrorMessage)) return;
    }

    public void CreateNewRecordFromModel(ProviderReviewModel model)
    {
        ProviderReviewId = model.ProviderReviewId.ToString();
        GuidQuery = model.GuidQuery;
        ReviewText = model.ReviewText;
        Rating = model.Rating;
        CreateDate = model.CreateDate;
        ProviderName = model.Provider.BusinessName;
    }
}
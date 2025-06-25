using System.Reflection;
using AMData.Models.CoreModels;

namespace AMData.Models.DTOModels;

public class ProviderPublicViewDTO : BaseDTO
{
    public string ProviderName { get; set; }
    public string ProviderDescription { get; set; }
    public List<ProviderReviewDTO> ProviderReviews { get; set; } = [];

    public void CreateNewRecordFromModels(ProviderModel provider)
    {
        ProviderName = provider.BusinessName;
        foreach (var review in provider.Reviews)
        {
            var dto = new ProviderReviewDTO();

            dto.CreateNewRecordFromModel(review);

            ProviderReviews.Add(dto);

            var className = MethodBase.GetCurrentMethod().DeclaringType.Name;
            var methodName = MethodBase.GetCurrentMethod().Name;

            ProviderDescription =
                "Capturing your most precious moments with creativity and passion. We specialize in timeless portraits, events, and professional photography tailored to tell your unique story.";
        }
    }
}
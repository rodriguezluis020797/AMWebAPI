using System.ComponentModel.DataAnnotations.Schema;
using AMData.Models.DTOModels;

namespace AMData.Models.CoreModels;

[Table("ProviderReview")]
public class ProviderReviewModel
{
    public ProviderReviewModel()
    {
    }

    public ProviderReviewModel(long providerId, long clientId)
    {
        ProviderId = providerId;
        ClientId = clientId;
        ReviewText = string.Empty;
        Rating = 0;
        GuidQuery = Guid.NewGuid().ToString();
        DeleteDate = null;
        CreateDate = DateTime.UtcNow;
    }

    public long ProviderReviewId { get; set; }
    public string GuidQuery { get; set; }
    [ForeignKey("Provider")] public long ProviderId { get; set; }
    [ForeignKey("Client")] public long ClientId { get; set; }
    public string ReviewText { get; set; }
    public decimal Rating { get; set; }
    public bool Submitted { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? DeleteDate { get; set; }

    [NotMapped] public virtual ProviderModel Provider { get; set; }
    [NotMapped] public virtual ClientModel Client { get; set; }

    public void UpdateRecordFromDto(ProviderReviewDTO dto)
    {
        ReviewText = dto.ReviewText;
        Rating = dto.Rating;
        Submitted = true;
    }
}
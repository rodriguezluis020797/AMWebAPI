using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels;

[Table("ProviderBilling")]
public class ProviderBillingModel
{
    public ProviderBillingModel()
    {
    }

    public ProviderBillingModel(long providerId, long amount, long discountAmount, DateTime dueDate)
    {
        ProviderId = providerId;
        Amount = amount;
        DiscountAmount = discountAmount;
        DueDate = dueDate;
        PaidDate = null;
        CreateDate = DateTime.UtcNow;
        UpdateDate = null;
        DeleteDate = null;
    }

    [Key] public long ProviderBillingId { get; set; }
    [ForeignKey("Provider")] public long ProviderId { get; set; }
    public long Amount { get; set; }
    public long DiscountAmount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public DateTime? DeleteDate { get; set; }
    [NotMapped] public ProviderModel Provider { get; set; }
}
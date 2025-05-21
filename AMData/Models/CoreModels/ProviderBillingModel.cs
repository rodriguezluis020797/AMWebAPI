using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels;

[Table("ProviderBilling")]
public class ProviderBillingModel(long providerId, long amount, long discountAmount, DateTime dueDate)
{
    [Key] public long ProviderBillingId { get; set; }
    [ForeignKey("Provider")] public long ProviderId { get; set; } = providerId;
    public long Amount { get; set; } = amount;
    public long DiscountAmount { get; set; } = discountAmount;
    public DateTime DueDate { get; set; } = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1);
    public DateTime? PaidDate { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdateDate { get; set; }
    public DateTime? DeleteDate { get; set; }
    [NotMapped] public ProviderModel Provider { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels;

[Table("ProviderLogPayment")]
public class ProviderLogPayment
{
    public ProviderLogPayment()
    {
    }

    public ProviderLogPayment(long providerId, bool success, string? comment)
    {
        ProviderId = providerId;
        Success = success;
        CreateDate = DateTime.UtcNow;
        Comment = comment;
    }

    [Key] public long ProviderLogPaymentId { get; set; }
    [ForeignKey("Provider")] public long ProviderId { get; set; }
    public bool Success { get; set; }
    public string ? Comment { get; set; }
    public DateTime CreateDate { get; set; }
    [NotMapped] public ProviderModel Provider { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels;

[Table("VerifyProviderEMailRequest")]
public class VerifyProviderEMailRequestModel
{
    public VerifyProviderEMailRequestModel(){}
    public VerifyProviderEMailRequestModel(long providerId)
    {
        ProviderId = providerId;
        QueryGuid = Guid.NewGuid().ToString();
        CreateDate = DateTime.UtcNow;
        DeleteDate = null;
    }
    
    [Key] public long VerifyProviderEMailRequestId { get; set; }
    [ForeignKey("Provider")] public long ProviderId { get; set; }
    public string QueryGuid { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? DeleteDate { get; set; }
    [NotMapped] public virtual ProviderModel Provider { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels;

[Table("ResetPasswordRequest")]
public class ResetPasswordRequestModel
{
    public ResetPasswordRequestModel()
    {
    }
    public ResetPasswordRequestModel(long providerId, string queryGuid)
    {
        ProviderId = providerId;
        QueryGuid = queryGuid;
        Reset = false;
        CreateDate = DateTime.UtcNow;
        UpdateDate = null;
        DeleteDate = null;
    }

    [Key] public long ResetPasswordId { get; set; }
    [ForeignKey("Provider")] public long ProviderId { get; set; }
    public string QueryGuid { get; set; }
    public bool Reset { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public DateTime? DeleteDate { get; set; }
    [NotMapped] public virtual ProviderModel Provider { get; set; }
}
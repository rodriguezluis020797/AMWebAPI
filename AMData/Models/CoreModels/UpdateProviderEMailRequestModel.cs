using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels
{
    [Table("UpdateProviderEMailRequest")]
    public class UpdateProviderEMailRequestModel
    {
        public UpdateProviderEMailRequestModel() { }
        public UpdateProviderEMailRequestModel(long providerId, string newEMail)
        {
            ProviderId = providerId;
            QueryGuid = Guid.NewGuid().ToString();
            NewEMail = newEMail;
            CreateDate = DateTime.UtcNow;
            DeleteDate = null;
        }

        [Key] public long UpdateProviderEMailRequestId { get; set; }
        [ForeignKey("Provider")] public long ProviderId { get; set; }
        public string QueryGuid { get; set; }
        public string NewEMail { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? DeleteDate { get; set; }
        [NotMapped] public virtual ProviderModel Provider { get; set; } = new ProviderModel();
    }
}

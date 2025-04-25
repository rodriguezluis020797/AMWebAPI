using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels
{
    [Table("Session")]
    public class SessionModel
    {
        public SessionModel() { }
        public SessionModel(long providerId)
        {
            ProviderId = providerId;
            CreateDate = DateTime.UtcNow;
        }

        [Key] public long SessionId { get; set; }
        [ForeignKey("Provider")] public long ProviderId { get; set; }
        public DateTime CreateDate { get; set; }
        [NotMapped] public virtual ProviderModel Provider { get; set; }
        [NotMapped] public virtual List<SessionActionModel> SessionActions { get; set; }
    }
}
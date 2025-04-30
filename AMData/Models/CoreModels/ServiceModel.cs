using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels
{
    [Table("Service")]
    public class ServiceModel
    {
        [Key] public long ServiceId { get; set; }
        [ForeignKey("Provider")] public long ProviderId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool AllowClientScheduling { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? DeleteDate { get; set; }
        [NotMapped] public virtual ProviderModel Provider { get; set; }
    }
}
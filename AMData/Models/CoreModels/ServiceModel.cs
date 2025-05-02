using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AMData.Models.DTOModels;

namespace AMData.Models.CoreModels
{
    [Table("Service")]
    public class ServiceModel
    {
        public ServiceModel(long providerId, string name, string description, bool allowClientScheduling, decimal price)
        {
            ProviderId = providerId;
            Name = name;
            Description = description;
            AllowClientScheduling = allowClientScheduling;
            Price = price;
            CreateDate = DateTime.UtcNow;
            DeleteDate = null;
        }
        [Key] public long ServiceId { get; set; }
        [ForeignKey("Provider")] public long ProviderId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool AllowClientScheduling { get; set; }
        public decimal Price { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? DeleteDate { get; set; }
        [NotMapped] public virtual ProviderModel Provider { get; set; }

        public void UpdateRecordFromDTO(ServiceDTO dto)
        {
            Name = dto.Name;
            Description = dto.Description;
            AllowClientScheduling = dto.AllowClientScheduling;
            Price = dto.Price;
        }
    }
}
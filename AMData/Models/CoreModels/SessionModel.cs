using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.CoreModels
{
    [Table("Session")]
    public class SessionModel
    {
        [Key] public long SessionId { get; set; }
        [ForeignKey("User")] public long UserId { get; set; }
        public DateTime CreateDate { get; set; }
        [NotMapped] public virtual ProviderModel User { get; set; }
        [NotMapped] public virtual List<SessionActionModel> SessionActions { get; set; }
    }
}
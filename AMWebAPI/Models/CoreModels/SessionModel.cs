using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMWebAPI.Models.CoreModels
{
    [Table("Session")]
    public class SessionModel
    {
        [Key] public long SessionId { get; set; }
        [ForeignKey("User")] public long UserId { get; set; }
        public DateTime CreateDate { get; set; }
        [NotMapped] public virtual UserModel User { get; set; }
    }
}
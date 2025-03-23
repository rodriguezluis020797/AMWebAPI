using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMWebAPI.Models.CoreModels
{
    [Table("Communication")]
    public class CommunicationModel
    {
        [Key] public long CommunicationId { get; set; }
        [ForeignKey("User")] public long UserId { get; set; }
        public string Message { get; set; }
        public DateTime SendAfter { get; set; }
        public DateTime? AttemptOne { get; set; }
        public DateTime? AttemptTwo { get; set; }
        public DateTime? AttemptThree { get; set; }
        public DateTime? DeleteDate { get; set; }
        [NotMapped] public virtual UserModel User { get; set; }
    }
}

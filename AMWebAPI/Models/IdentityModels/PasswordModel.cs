using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMWebAPI.Models.IdentityModels
{
    [Table("Password")]
    public class PasswordModel
    {
        [Key] public long PasswordId { get; set; }
        public long UserId { get; set; }
        public bool Temporary { get; set; }
        public string HashedPassword { get; set; }
        public string Salt { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? DeleteDate { get; set; }
    }
}

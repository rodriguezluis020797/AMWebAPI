using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.IdentityModels
{
    [Table("RefreshToken")]
    public class RefreshTokenModel
    {
        [Key] public long RefreshTokenId { get; set; }
        public long UserId { get; set; }
        public string Token { get; set; }
        public string FingerPrint {  get; set; } 
        public DateTime ExpiresDate { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? DeleteDate { get; set; }
    }
}
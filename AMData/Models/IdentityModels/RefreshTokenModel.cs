using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMData.Models.IdentityModels
{
    [Table("RefreshToken")]
    public class RefreshTokenModel
    {
        [Key] public long RefreshTokenId { get; set; }
        public long ProviderId { get; set; }
        public string Token { get; set; }
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
        public string Platform { get; set; }
        public string Language { get; set; }
        public DateTime ExpiresDate { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? DeleteDate { get; set; }
    }
}